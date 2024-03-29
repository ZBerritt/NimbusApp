﻿using System.Collections;
using System.Windows.Forms;

namespace NimbusApp.UI
{
    internal class SaveListSorter : IComparer
    {
        // Sort remote saves below local saves
        public int Compare(object x, object y)
        {
            ListViewItem item1 = (ListViewItem)x;
            ListViewItem item2 = (ListViewItem)y;
            if (item1.SubItems[1].Text == "Remote")
            {
                if (item2.SubItems[1].Text == "Remote")
                {
                    return string.Compare(item1.Text, item2.Text);
                }
                return 1;
            }
            if (item2.SubItems[1].Text == "Remote")
            {
                return -1;
            }
            return string.Compare(item1.Text, item2.Text);
        }
    }
}