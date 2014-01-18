using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DDDWorkshop.Model.Issues
{
    class IssueFixed
    {
        private IssueId _id;
        private string resolution;

        public IssueFixed(IssueId _id, string resolution)
        {
            // TODO: Complete member initialization
            this._id = _id;
            this.resolution = resolution;
        }
    }
}
