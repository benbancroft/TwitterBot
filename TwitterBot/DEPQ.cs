using System;
using System.Linq;
using System.Collections.Generic;

namespace TwitterBot
{
	public class DEPQ<T> where T : IComparable<T>
	{

		public DEPQ ()
		{
			this.heap = new List<Node<T>> ();
		}

		public DEPQ (int size)
		{
			this.heap = new List<Node<T>> (size);
		}

		/**
     	* Class for storing Node instance in heap.
     	*/
		private class Node<NT>
		{
			public Node (NT leftInterval, NT rightInterval)
			{
				this.leftInterval = leftInterval;
				this.rightInterval = rightInterval;
			}

			public NT leftInterval { get; set; }

			public NT rightInterval { get; set; }
		}

		//Implemented as an ArrayList of nodes. Space complexity O(n).
		private List<Node<T>> heap { get; set; }

		private int intervalHeapSize = 0;

		/**
     * Helper method for swapping nodes
     * @param item1 First item to swap
     * @param item2 Second item to swap
     */
		private void swapLeftLeft (Node<T> item1, Node<T> item2)
		{
			T temp = item2.leftInterval;
			item2.leftInterval = item1.leftInterval;
			item1.leftInterval = temp;
		}

		/**
     * Helper method for swapping nodes
     * @param item1 First item to swap
     * @param item2 Second item to swap
     */
		private void swapRightRight (Node<T> item1, Node<T> item2)
		{
			T temp = item1.rightInterval;
			item1.rightInterval = item2.rightInterval;
			item2.rightInterval = temp;
		}

		/**
     * Helper method for swapping nodes
     * @param item1 First item to swap
     * @param item2 Second item to swap
     */
		private void swapLeftRight (Node<T> item1, Node<T> item2)
		{
			T temp = item1.leftInterval;
			item1.leftInterval = item2.rightInterval;
			item2.rightInterval = temp;
		}

		/**
     * Helper method to get parent index from heap
     * @param index Index of node you want parent index for
     * @return Parent index
     */
		private int getParentIndex (int index)
		{
			return (index + 1) / 2 - 1;
		}

		/**
     * Helper method to get child index from heap
     * @param index Index of node you want child index for
     * @return Child index
     */
		private int getChildIndex (int index)
		{
			return (index + 1) * 2 - 1;
		}

		/**
     * O(log n)
     * Handles the bubble up operation need for min heap insert. Moves an item up the heap based on it being smaller than the left interval.
     * The intervals are swapped until this is not the case or the root node is found.
     */
		private void minHeapBubbleUp ()
		{

			int index = heap.Count - 1;
			Node<T> currentNode = heap [index];

			//move up the heap until we get to zero index
			while (index > 0) {
				int parentIndex = getParentIndex (index);
				Node<T> parentNode = heap [parentIndex];

				//Handle edge case where parent is bigger than current. If so we can stop at this point.
				if (currentNode.leftInterval.CompareTo (parentNode.leftInterval) >= 0)
					break;

				//If not we swap and carry on up
				swapLeftLeft (currentNode, parentNode);

				index = parentIndex;
				currentNode = parentNode;
			}
		}

		/**
     * O(log n)
     * Handles the bubble up operation need for max heap insert. Moves an item up the heap based on it being bigger than the right interval.
     * The intervals are swapped until this is not the case or the root node is found. If the right interval is not set, then swap them and carry on.
     */
		private void maxHeapBubbleUp ()
		{

			int index = heap.Count - 1;
			Node<T> currentNode = heap [index];

			//move up the heap until we get to zero index
			while (index > 0) {
				int parentIndex = getParentIndex (index);
				Node<T> parentNode = heap [parentIndex];

				//Case for when this node only has a left interval
				if (currentNode.rightInterval == null) {
					//compare left interval with parents right interval. If its smaller then we can bail here as heap is correct.
					if (currentNode.leftInterval.CompareTo (parentNode.rightInterval) < 0)
						break;

					//If not we swap
					swapLeftRight (currentNode, parentNode);

					index = parentIndex;
					currentNode = parentNode;
					//case for when there is a right interval
				} else {
					//check that the right interval is less than that of the parent
					if (currentNode.rightInterval.CompareTo (parentNode.rightInterval) < 0)
						break;

					//If not less swap them
					swapRightRight (currentNode, parentNode);

					index = parentIndex;
					currentNode = parentNode;
				}
			}

		}

		/**
     * O(log n)
     * Handles the bubble down operation need for min heap insert. Repairs heap after an item is removed so that the interval relationship works.
     * Iterate over the children elements, moving the left interval up. If the right interval is set it will also move this into the left and carry on until it reaches the bottom.
     */
		private void minHeapBubbleDown ()
		{
			int index = 0;
			Node<T> currentNode = heap [index];

			//move down the heap
			while (getChildIndex (index) < heap.Count) {

				int childIndex = getChildIndex (index);

				//check if sibling nodes exist for child
				if (childIndex + 1 < heap.Count) {
					//if the child's left interval is smaller than the siblings left, we move to that sibling
					if (heap [childIndex].leftInterval.CompareTo (heap [childIndex + 1].leftInterval) >= 0)
						childIndex++;
				}

				Node<T> child = heap [childIndex];
				//if the child's left is bigger than the parent, we can bail here
				if (currentNode.leftInterval.CompareTo (child.leftInterval) < 0)
					break;

				//else we swap the values
				swapLeftLeft (child, currentNode);

				//if the child's intervals are the wrong way round, we swap them
				if (child.rightInterval != null && child.leftInterval.CompareTo (child.rightInterval) > 0) {
					swapLeftRight (child, child);
				}

				index = childIndex;
				currentNode = child;
			}
		}

		/**
     * O(log n)
     * Handles the bubble down operation need for max heap insert. If the item is the parent of the last item in the heap then check if its in the left or right interval.
     * If this is the case then compare the right interval of the parent with the left of the child. If the left of the child is less then skip to the child.
     * If not then check the right of both the parent and child. If the child is less then the parent then skip to the child.
     * If the left interval is more than the right interval then swap them.
     * If the child's right interval doesn't exist and is less than the parents left then swap them.
     */
		private void maxHeapBubbleDown ()
		{

			int index = 0;
			Node<T> currentNode = heap [index];

			//move down the heap
			while (getChildIndex (index) < heap.Count) {

				int childIndex = getChildIndex (index);

				//if siblings exist for the child
				if (childIndex + 1 < heap.Count) {

					//check if we should move to the right child
					if (Size () % 2 == 1 && childIndex + 1 == heap.Count - 1) {
						//case for when the right sibling is last element, and only has one interval
						if (heap [childIndex].rightInterval.CompareTo (heap [childIndex + 1].leftInterval) <= 0)
							childIndex++;
					} else {
						//other case when right sibling has two intervals
						if (heap [childIndex].rightInterval.CompareTo (heap [childIndex + 1].rightInterval) <= 0)
							childIndex++;
					}
				}

				Node<T> child = heap [childIndex];
				//case for when node only has one interval
				if (child.rightInterval == null) {

					//bail case for when intervals are correct way around
					if (child.leftInterval.CompareTo (currentNode.rightInterval) < 0)
						break;

					//if not swap
					swapLeftRight (child, currentNode);
				}
				//other case
				else {
					//bubble down logic - check if parent nodes right interval is bigger than that of the child
					if (child.rightInterval.CompareTo (currentNode.rightInterval) < 0)
						break;

					//if not swap
					swapRightRight (currentNode, child);

					//if we have done a swap, we now need to check child's intervals are correct way around
					if (child.leftInterval.CompareTo (child.rightInterval) > 0) {

						//if not swap
						swapLeftRight (child, child);
					}
				}

				index = childIndex;
				currentNode = child;
			}

		}

		public bool Contains (T element)
		{
			return this.heap.Any (i => (i.leftInterval != null && i.leftInterval.Equals (element)) || (i.rightInterval != null && i.rightInterval.Equals (element)));
		}

		/**
     * O(1)
     * As direct read from index, constant time.
     * Return the smallest item from the heap without removing it
     * @return Smallest item from heap
     */
		public T InspectLeast ()
		{
			if (IsEmpty ())
				return default(T);

			return heap [0].leftInterval;
		}

		/**
     * log(n)
     * As direct read from index, constant time.
     * Return the biggest item from the heap without removing it
     * @return Biggest item from heap
     */
		public T InspectMost ()
		{
			if (IsEmpty ())
				return default(T);

			Node<T> rootNode = heap [0];
			if (Size () == 1)
				return rootNode.leftInterval;
			else
				return rootNode.rightInterval;
		}

		/**
     * O(log n)
     * Logarithmic time as need to bubble up heap, and so in worse case will have to go up whole heap.
     * Having said this, its implemented using underlying ArrayList, which when expands will be O(n)
     * Add new item to the heap, and bubble it to the correct place
     * @param element Element to add to the heap
     */
		public void Add (T element)
		{
			if (Size () % 2 == 0) {

				heap.Add (new Node<T> (element, default(T)));

			} else {
				Node<T> node = heap [heap.Count - 1];

				if (node.leftInterval.CompareTo (element) > 0) {
					node.rightInterval = node.leftInterval;
					node.leftInterval = element;
				} else {
					node.rightInterval = element;
				}
			}

			intervalHeapSize++;

			if (Size () <= 2)
				return;

			//get the parent node from tree with a bit of binary maths
			Node<T> parentNode = heap [getParentIndex (heap.Count - 1)];

			//check if element exceeds the bounds of parent node
			if (parentNode.leftInterval.CompareTo (element) > 0) {
				minHeapBubbleUp ();
			} else if (parentNode.rightInterval.CompareTo (element) < 0) {
				maxHeapBubbleUp ();
			}
		}

		/**
     * O(log n)
     * Logarithmic time as need to bubble down heap, and so in worse case will have to go down whole heap.
     * Get the smallest item from the heap, and remove it from the heap
     * @return Smallest item in the heap
     */
		public T GetLeast ()
		{
			T element = InspectLeast ();

			if (EqualityComparer<T>.Default.Equals (element, default(T)))
				return default(T);

			if (Size () == 1) {
				heap.RemoveAt (0);
				intervalHeapSize--;
				return element;
			}

			Node<T> lastNode = heap [heap.Count - 1];
			heap [0].leftInterval = lastNode.leftInterval;

			if (Size () % 2 == 1) {
				heap.RemoveAt (heap.Count - 1);
			} else {
				lastNode.leftInterval = lastNode.rightInterval;
				lastNode.rightInterval = default(T);
			}
			intervalHeapSize--;

			minHeapBubbleDown ();

			return element;
		}

		/**
     * O(log n)
     * Logarithmic time as need to bubble down heap, and so in worse case will have to go down whole heap.
     * Get the largest item from the heap, and remove it from the heap
     * @return Largest item in the heap
     */
		public T GetMost ()
		{
			T element = InspectMost ();

			if (EqualityComparer<T>.Default.Equals (element, default(T)))
				return default(T);

			if (Size () == 1) {
				heap.RemoveAt (0);
				intervalHeapSize--;
				return element;
			}

			Node<T> lastNode = heap [heap.Count - 1];

			if (Size () % 2 == 1) {
				heap [0].rightInterval = lastNode.leftInterval;
				heap.RemoveAt (heap.Count - 1);
			} else {
				heap [0].rightInterval = lastNode.rightInterval;
				lastNode.rightInterval = default(T);
			}
			intervalHeapSize--;

			maxHeapBubbleDown ();

			return element;
		}

		/**
     * O(1)
     * Checking variable is constant time, as can be read directly - same as size()
     * Return a boolean state if the heap has no items or not
     * @return Boolean state for if the heap is empty or not
     */
		public bool IsEmpty ()
		{
			return Size () == 0;
		}

		/**
     * O(1)
     * Checking variable is constant time, as can be read directly
     * Return the number of items in the heap
     * @return Number of items in the heap
     */
		public int Size ()
		{
			return intervalHeapSize;
		}
	}

}

