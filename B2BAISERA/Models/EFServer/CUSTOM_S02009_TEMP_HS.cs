//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace B2BAISERA.Models.EFServer
{
    public partial class CUSTOM_S02009_TEMP_HS
    {
        #region Primitive Properties
    
        public virtual int ID
        {
            get;
            set;
        }
    
        public virtual Nullable<int> TransactionDataID
        {
            get { return _transactionDataID; }
            set
            {
                try
                {
                    _settingFK = true;
                    if (_transactionDataID != value)
                    {
                        if (CUSTOM_TRANSACTIONDATA != null && CUSTOM_TRANSACTIONDATA.ID != value)
                        {
                            CUSTOM_TRANSACTIONDATA = null;
                        }
                        _transactionDataID = value;
                    }
                }
                finally
                {
                    _settingFK = false;
                }
            }
        }
        private Nullable<int> _transactionDataID;
    
        public virtual string PONumber
        {
            get;
            set;
        }
    
        public virtual Nullable<decimal> VersionPOSERA
        {
            get;
            set;
        }
    
        public virtual Nullable<decimal> DataVersion
        {
            get;
            set;
        }
    
        public virtual string dibuatOleh
        {
            get;
            set;
        }
    
        public virtual Nullable<System.DateTime> dibuatTanggal
        {
            get;
            set;
        }
    
        public virtual string diubahOleh
        {
            get;
            set;
        }
    
        public virtual Nullable<System.DateTime> diubahTanggal
        {
            get;
            set;
        }
    
        public virtual string CompanyCodeAI
        {
            get;
            set;
        }
    
        public virtual string KodeCabangAI
        {
            get;
            set;
        }

        #endregion

        #region Navigation Properties
    
        public virtual CUSTOM_TRANSACTIONDATA CUSTOM_TRANSACTIONDATA
        {
            get { return _cUSTOM_TRANSACTIONDATA; }
            set
            {
                if (!ReferenceEquals(_cUSTOM_TRANSACTIONDATA, value))
                {
                    var previousValue = _cUSTOM_TRANSACTIONDATA;
                    _cUSTOM_TRANSACTIONDATA = value;
                    FixupCUSTOM_TRANSACTIONDATA(previousValue);
                }
            }
        }
        private CUSTOM_TRANSACTIONDATA _cUSTOM_TRANSACTIONDATA;

        #endregion

        #region Association Fixup
    
        private bool _settingFK = false;
    
        private void FixupCUSTOM_TRANSACTIONDATA(CUSTOM_TRANSACTIONDATA previousValue)
        {
            if (previousValue != null && previousValue.CUSTOM_S02009_TEMP_HS.Contains(this))
            {
                previousValue.CUSTOM_S02009_TEMP_HS.Remove(this);
            }
    
            if (CUSTOM_TRANSACTIONDATA != null)
            {
                if (!CUSTOM_TRANSACTIONDATA.CUSTOM_S02009_TEMP_HS.Contains(this))
                {
                    CUSTOM_TRANSACTIONDATA.CUSTOM_S02009_TEMP_HS.Add(this);
                }
                if (TransactionDataID != CUSTOM_TRANSACTIONDATA.ID)
                {
                    TransactionDataID = CUSTOM_TRANSACTIONDATA.ID;
                }
            }
            else if (!_settingFK)
            {
                TransactionDataID = null;
            }
        }

        #endregion

    }
}