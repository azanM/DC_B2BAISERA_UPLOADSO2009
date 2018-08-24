using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace B2BAISERA.Models
{
    class S02009HSISViewModel
    {
        public string HSIS
        {
            get;
            set;
        }

        public string PONumber
        {
            get;
            set;
        }


        public Nullable<decimal> VersionPOSERA
        {
            get;
            set;
        }

        public Nullable<int> DataVersion
        {
            get;
            set;
        }

        public string ChassisNumberByVendor
        {
            get;
            set;
        }

        public Nullable<System.DateTime> GRDATE
        {
            get;
            set;
        }

    }
}
