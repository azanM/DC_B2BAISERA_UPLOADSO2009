using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace B2BAISERA.Models
{
    public class S02009ISViewModel
    {
        public int ID
        {
            get;
            set;
        }

        public int TransactionDataID
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

        public Nullable<decimal> DataVersion
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
