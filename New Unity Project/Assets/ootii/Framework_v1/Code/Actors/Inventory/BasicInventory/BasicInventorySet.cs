using System;
using System.Collections.Generic;

namespace com.ootii.Actors.Inventory
{
    /// <summary>
    /// An inventory set is a collection of items that go together. For each
    /// item, there is a slot that the item could belong to.
    /// </summary>
    [Serializable]
    public class BasicInventorySet
    {
        /// <summary>
        /// ID of the inventory set that should be unique for the inventory
        /// </summary>
        public string _ID = "";
        public string ID
        {
            get { return _ID; }
            set { _ID = value; }
        }

        /// <summary>
        /// List of item descriptors the set contains
        /// </summary>
        public List<BasicInventorySetItem> Items = new List<BasicInventorySetItem>();
    }
}
