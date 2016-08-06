﻿using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace TwitterBot
{
	[TestFixture ()]
	public class DEPQTest
	{

		private DEPQ<string> depq;

		private static char[] letters = {'a', 'b', 'c', 'd', 'e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z'};

		private Random r;

		private string randomString(int len) {
			char[] word = new char[len];
			for (int i=0; i<len; i++) word[i] = letters[r.Next(0, letters.Length)];
			string str = new string(word);

			return str;
		}

		[SetUp]
		public void setUp(){
			depq = new DEPQ<string> ();
			r = new Random(Guid.NewGuid().GetHashCode());
		}

		 /**
	     * Test of inspectLeast method, of class DEPQ.
	     */
		[Test ()]
		public void testInspectLeast() {
			Console.WriteLine("inspectLeast");

			List<string> array = new List<string>();
			string smallest = "zzzzz";
			// First check adding random number gives correct smallest
			for (int i=0; i<100; i++) {

				string kstr = randomString(smallest.Length);
				array.Add(kstr);
				depq.add(kstr);
				if (kstr.CompareTo(smallest)<0) smallest = kstr;

				string result = depq.inspectLeast();
				Assert.AreEqual(smallest, result);

			}

			// Next randomly add or remove and check inspect least
			for (int i=0; i<99; i++) {
				bool add = r.Next(0, 1)>0.5;
				if (add) {
					string kstr = randomString(smallest.Length);

					array.Add(kstr);
					depq.add(kstr);
					if (kstr.CompareTo(smallest)<0) smallest = kstr;
				} else {
					string discarded = depq.getLeast();
					array.Remove(discarded);
					smallest = array[0];
					for(int j=1; j<array.Count; j++) {
						if (array[j].CompareTo(smallest)<0) smallest = array[j];
					}
				}

				string result = (string)depq.inspectLeast();
				Assert.AreEqual(smallest, result);
			}
		}

		/**
     	* Test of inspectMost method, of class DEPQ.
     	*/
		[Test ()]
		public void testInspectMost() {
			Console.WriteLine("inspectMost");

			List<string> array = new List<string>();
			string largest = "a";
			// First check adding random number gives correct largest
			for (int i=0; i<100; i++) {
				string kstr = randomString(5);

				array.Add(kstr);
				depq.add(kstr);
				if (kstr.CompareTo(largest)>0) largest = kstr;

				string result = depq.inspectMost();
				Assert.AreEqual(largest, result);

			}

			// Next randomly add or remove and check inspect most
			for (int i=0; i<99; i++) {
				bool add = r.Next(0, 1)>0.5;
				if (add) {
					string kstr = randomString(5);

					array.Add(kstr);
					depq.add(kstr);
					if (kstr.CompareTo(largest)>0) largest = kstr;
				} else {
					string discarded = depq.getMost();
					array.Remove(discarded);
					largest = array[0];
					for(int j=1; j<array.Count; j++) {
						if (array[j].CompareTo(largest)>0) largest = array[j];
					}
				}

				string result = (string)depq.inspectMost();
				Assert.AreEqual(largest, result);
			}
		}

		/**
	     * Test of add method, of class DEPQ.
	     */
		[Test ()]
		public void testAdd() {
			Console.WriteLine("add");

			for (int i=0; i<1000; i++) {
				string kstr = randomString(5);

				depq.add(kstr);
				Assert.AreEqual(i+1, depq.size());
			}

		}
			
		/**
	     * Test of contains method, of class DEPQ.
	     */
		[Test ()]
		public void testContains() {
			Console.WriteLine("contains");

			List<string> array = new List<string>();

			for (int i=0; i<1000; i++) {

				while(true) {
					string kstr = randomString (5);
					if (array.Contains (kstr)) continue;

					array.Add (kstr);
					depq.add (kstr);

					Assert.AreEqual (i + 1, depq.size ());

					Assert.IsTrue (depq.Contains (kstr));

					break;

				}
			}

		}

		/**
	     * Test of getLeast method, of class DEPQ.
	     */
		[Test ()]
		public void testGetLeast() {
			Console.WriteLine("getLeast");

			for (int i=0; i<1000; i++) {
				depq.add(randomString(5));
			}
			for (int i=0; i<1000; i++) {
				string expResult = (String)depq.inspectLeast();
				string result = (String)depq.getLeast();
				Assert.AreEqual(expResult, result);
			}
		}

		/**
	     * Test of getMost method, of class DEPQ.
	     */
		[Test ()]
		public void testGetMost() {
			Console.WriteLine("getMost");

			for (int i=0; i<1000; i++) {
				depq.add(randomString(5));
			}
			for (int i=0; i<1000; i++) {
				string expResult = (String)depq.inspectMost();
				string result = (String)depq.getMost();
				Assert.AreEqual(expResult, result);
			}
		}

		/**
	     * Test of isEmpty method, of class DEPQ.
	     */
		[Test ()]
		public void testIsEmpty() {
			Console.WriteLine("isEmpty");

			bool expResult = true;
			bool result = depq.isEmpty();
			Assert.AreEqual(expResult, result);

			for (int i=0; i<10; i++) {
				int count = r.Next(0, 1000);
				for (int j=0; j<count; j++) {
					depq.add(""+j);
					Assert.AreEqual(false, depq.isEmpty());
				}
				for (int j=0; j<count; j++) {
					Assert.AreEqual(false, depq.isEmpty());
					depq.getLeast();
				}
				Assert.AreEqual(true, depq.isEmpty());
			}
		}

		/**
	     * Test of size method, of class DEPQ.
	     */
		[Test ()]
		public void testSize() {
			Console.WriteLine("size");

			for (int i=0; i<1000; i++) {
				int k = r.Next(0, 100);
				depq.add(""+k);
				Assert.AreEqual(i+1, depq.size());
			}

			for (int i=0; i<1000; i++) {
				bool bigEnd = r.Next(0, 1)>0.5;
				if (bigEnd) depq.getMost();
				else depq.getLeast();
				Assert.AreEqual(1000-i-1, depq.size());
			}
		}

	}

}

