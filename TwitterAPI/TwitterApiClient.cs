using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Web;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Reflection;

using TwitterAPI.Dtos;
using TwitterAPI.Dtos.Annotations;
using TwitterAPI.Exceptions;

namespace TwitterAPI
{
	public class TwitterApiClient
	{
		public const string Realm = "Twitter API";

		private static readonly string[] SecretParameters = new[] {
			"oauth_consumer_secret",
			"oauth_token_secret",
			"oauth_signature"
		};

		private static readonly string[] OAuthParametersToIncludeInHeader = new[] {
			"oauth_version",
			"oauth_nonce",
			"oauth_timestamp",
			"oauth_signature_method",
			"oauth_consumer_key",
			"oauth_token",
			"oauth_verifier"
			// Leave signature omitted from the list, it is added manually
			// "oauth_signature",
		};

		public OAuthTokens Tokens { get; set; }

		public TwitterApiClient (OAuthTokens tokens)
		{
			this.Tokens = tokens;
		}

		public static string GenerateTimeStamp ()
		{
			// Default implementation of UNIX time of the current UTC time
			TimeSpan ts = DateTime.UtcNow - new DateTime (1970, 1, 1, 0, 0, 0, 0);
			return Convert.ToInt64 (ts.TotalSeconds, CultureInfo.CurrentCulture).ToString (CultureInfo.CurrentCulture);
		}

		public static string GenerateNonce ()
		{
			// Just a simple implementation of a random number between 123400 and 9999999
			return new Random ()
				.Next (123400, int.MaxValue)
				.ToString ("X", CultureInfo.InvariantCulture);
		}

		public string GenerateSignature (Uri requestUri, HttpMethod requestMethod, IEnumerable<KeyValuePair<string, object>> parameters)
		{
			IEnumerable<KeyValuePair<string, object>> nonSecretParameters = (from p in parameters
			                                                                 where (!SecretParameters.Contains (p.Key))
			                                                                 select p);

			// Create the base string. This is the string that will be hashed for the signature.
			string signatureBaseString = string.Format (
				                             CultureInfo.InvariantCulture,
				                             "{0}&{1}&{2}",
				                             requestMethod.ToString ().ToUpper (CultureInfo.InvariantCulture),
				                             UrlEncode (requestUri.AbsoluteUri),
				                             UrlEncode (nonSecretParameters));

			// Create our hash key (you might say this is a password)
			string key = string.Format (
				             CultureInfo.InvariantCulture,
				             "{0}&{1}",
				             UrlEncode (this.Tokens.ConsumerSecret),
				             UrlEncode (this.Tokens.AccessTokenSecret));


			// Generate the hash
			HMACSHA1 hmacsha1 = new HMACSHA1 (Encoding.UTF8.GetBytes (key));
			byte[] signatureBytes = hmacsha1.ComputeHash (Encoding.UTF8.GetBytes (signatureBaseString));
			return Convert.ToBase64String (signatureBytes);
		}

		public static string UrlEncode (string value)
		{
			if (string.IsNullOrEmpty (value)) {
				return string.Empty;
			}

			value = Uri.EscapeDataString (value);

			// UrlEncode escapes with lowercase characters (e.g. %2f) but oAuth needs %2F
			value = Regex.Replace (value, "(%[0-9a-f][0-9a-f])", c => c.Value.ToUpper ());

			// these characters are not escaped by UrlEncode() but needed to be escaped
			value = value
				.Replace ("(", "%28")
				.Replace (")", "%29")
				.Replace ("$", "%24")
				.Replace ("!", "%21")
				.Replace ("*", "%2A")
				.Replace ("'", "%27");

			// these characters are escaped by UrlEncode() but will fail if unescaped!
			value = value.Replace ("%7E", "~");

			return value;
		}

		private static string UrlEncode (IEnumerable<KeyValuePair<string, object>> parameters)
		{
			StringBuilder parameterString = new StringBuilder ();

			var paramsSorted = from p in parameters
			                   orderby p.Key, p.Value
			                   select p;

			foreach (var item in paramsSorted) {
				if (item.Value is string) {
					if (parameterString.Length > 0) {
						parameterString.Append ("&");
					}

					parameterString.Append (
						string.Format (
							CultureInfo.InvariantCulture,
							"{0}={1}",
							(item.Key),
							UrlEncode ((string)item.Value)));
				}
			}

			return UrlEncode (parameterString.ToString ());
		}

		public string GenerateAuthorizationHeader (Dictionary<string, object> parameters)
		{
			StringBuilder authHeaderBuilder = new StringBuilder ();
			authHeaderBuilder.AppendFormat ("OAuth realm=\"{0}\"", Realm);

			var sortedParameters = from p in parameters
			                       where OAuthParametersToIncludeInHeader.Contains (p.Key)
			                       orderby p.Key, UrlEncode( (p.Value is string) ? (string)p.Value : string.Empty)
			                       select p;

			foreach (var item in sortedParameters) {
				authHeaderBuilder.AppendFormat (
					",{0}=\"{1}\"",
					UrlEncode (item.Key),
					UrlEncode (item.Value as string));
			}

			authHeaderBuilder.AppendFormat (",oauth_signature=\"{0}\"", UrlEncode (parameters ["oauth_signature"] as string));

			return authHeaderBuilder.ToString ();
		}

		private void AddQueryStringParametersToUri (ref Uri requestUri, IEnumerable<KeyValuePair<string, object>> parameters)
		{
			StringBuilder requestParametersBuilder = new StringBuilder (requestUri.AbsoluteUri);
			requestParametersBuilder.Append (requestUri.Query.Length == 0 ? "?" : "&");


			Dictionary<string, object> fieldsToInclude = new Dictionary<string, object> (parameters.Where (p => !OAuthParametersToIncludeInHeader.Contains (p.Key) &&
			                                             !SecretParameters.Contains (p.Key)).ToDictionary (p => p.Key, p => p.Value));

			foreach (KeyValuePair<string, object> item in fieldsToInclude) {
				if (item.Value is string)
					requestParametersBuilder.AppendFormat ("{0}={1}&", item.Key, UrlEncode ((string)item.Value));
			}        

			if (requestParametersBuilder.Length == 0)
				return;

			requestParametersBuilder.Remove (requestParametersBuilder.Length - 1, 1);

			requestUri = new Uri (requestParametersBuilder.ToString ());
		}

		public TResponse request<TResponse> (IReturn<TResponse> request)
		{

			Route route = (Route)Attribute.GetCustomAttribute (request.GetType (), typeof(Route));

			if (route == null || route.Uri == null)
				throw new InvalidRouteException ();

			var parameters = new Dictionary<string, object> ();

			parameters.Add ("oauth_version", "1.0");
			parameters.Add ("oauth_nonce", GenerateNonce ());
			parameters.Add ("oauth_timestamp", GenerateTimeStamp ());
			parameters.Add ("oauth_signature_method", "HMAC-SHA1");
			parameters.Add ("oauth_consumer_key", this.Tokens.ConsumerKey);
			parameters.Add ("oauth_consumer_secret", this.Tokens.ConsumerSecret);

			parameters.Add ("oauth_token", this.Tokens.AccessToken);
			parameters.Add ("oauth_token_secret", this.Tokens.AccessTokenSecret);

			var uri = route.Uri;

			var argumentRegex = new Regex (":(.*)$", RegexOptions.None);
			var argumentMatch = argumentRegex.Match (uri);

			PropertyInfo[] properties = request.GetType ().GetProperties ();
			foreach (PropertyInfo property in properties) {
				var propValue = property.GetValue (request);
				object[] attributes = property.GetCustomAttributes (true);
				foreach (object attribute in attributes) {
					Parameter paramAttribute = attribute as Parameter;
					if (paramAttribute != null) {
						if (paramAttribute.Required && propValue == null)
							throw new InvalidParameterException ();

						if (argumentMatch.Success && argumentMatch.Value == ":" + paramAttribute.Name) {
							var matchEnd = argumentMatch.Index + argumentMatch.Value.Length;
							uri = uri.Substring (0, argumentMatch.Index) + uri.Substring (matchEnd, uri.Length - matchEnd) + propValue.ToString();
						}else if (propValue != null) parameters.Add (paramAttribute.Name, UrlEncode (propValue.ToString ()));
					}
				}
			}

			var requestUri = new Uri ("https://api.twitter.com/1.1/" + uri + ".json");
			var requestMethod = route.Method;

			// Add the signature to the oauth parameters
			parameters.Add ("oauth_signature", GenerateSignature (requestUri, requestMethod, parameters));

			// Do the timeline
			this.AddQueryStringParametersToUri (ref requestUri, parameters);
			HttpWebRequest httpRequest = WebRequest.Create (requestUri.AbsoluteUri) as HttpWebRequest;
			httpRequest.Accept = "*/*";
			httpRequest.Headers.Add ("Authorization", GenerateAuthorizationHeader (parameters));
			httpRequest.Method = requestMethod.ToString ();

			var jsonSettings = new JsonSerializerSettings {
				DateFormatString = "ddd MMM dd HH:mm:ss +ffff yyyy",
				ContractResolver = new UnderscorePropertyNamesContractResolver ()
			};

			try {

				HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse ();

				var jsonResponse = string.Empty;
				using (httpResponse) {
					using (var reader = new StreamReader (httpResponse.GetResponseStream ())) {
						jsonResponse = reader.ReadToEnd ();
					}
				}

				return JsonConvert.DeserializeObject<TResponse> (jsonResponse, jsonSettings);

			}catch(WebException ex){
				HttpWebResponse httpResponse = (HttpWebResponse)ex.Response;

				var jsonResponse = string.Empty;
				using (httpResponse) {
					using (var reader = new StreamReader (httpResponse.GetResponseStream ())) {
						jsonResponse = reader.ReadToEnd ();
					}
				}

				var errors = JsonConvert.DeserializeObject<ListErrorResponse> (jsonResponse, jsonSettings);

				throw new ApiException(errors, httpResponse.StatusCode);
			}
		}
	}

	public class OAuthTokens
	{
		/// <summary>
		/// Gets or sets the access token.
		/// </summary>
		/// <value>The access token.</value>
		public string AccessToken { internal get; set; }

		/// <summary>
		/// Gets or sets the access token secret.
		/// </summary>
		/// <value>The access token secret.</value>
		public string AccessTokenSecret { internal get; set; }

		/// <summary>
		/// Gets or sets the consumer key.
		/// </summary>
		/// <value>The consumer key.</value>
		public string ConsumerKey { internal get; set; }

		/// <summary>
		/// Gets or sets the consumer secret.
		/// </summary>
		/// <value>The consumer secret.</value>
		public string ConsumerSecret { internal get; set; }

	}

	public class UnderscorePropertyNamesContractResolver : DefaultContractResolver
	{
		public UnderscorePropertyNamesContractResolver () : base ()
		{
		}

		protected override string ResolvePropertyName (string propertyName)
		{
			return Regex.Replace (propertyName, @"(\w)([A-Z])", "$1_$2").ToLower ();
		}
	}
}

