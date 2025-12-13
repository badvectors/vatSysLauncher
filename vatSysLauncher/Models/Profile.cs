namespace vatSysLauncher.Models
{
    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    [System.Xml.Serialization.XmlRoot(Namespace = "", IsNullable = false)]
    public partial class Profile
    {

        private ProfileVersion versionField;

        private ProfileServers serversField;

        private string nameField;

        private string fullNameField;

        /// <remarks/>
        public ProfileVersion Version
        {
            get
            {
                return versionField;
            }
            set
            {
                versionField = value;
            }
        }

        /// <remarks/>
        public ProfileServers Servers
        {
            get
            {
                return serversField;
            }
            set
            {
                serversField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string Name
        {
            get
            {
                return nameField;
            }
            set
            {
                nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string FullName
        {
            get
            {
                return fullNameField;
            }
            set
            {
                fullNameField = value;
            }
        }
    }

    /// <remarks/>
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class ProfileVersion
    {

        private string aIRACField;

        private string revisionField;

        private uint publishDateField;

        private string updateURLField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string AIRAC
        {
            get
            {
                return aIRACField;
            }
            set
            {
                aIRACField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string Revision
        {
            get
            {
                return revisionField;
            }
            set
            {
                revisionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public uint PublishDate
        {
            get
            {
                return publishDateField;
            }
            set
            {
                publishDateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string UpdateURL
        {
            get
            {
                return updateURLField;
            }
            set
            {
                updateURLField = value;
            }
        }
    }

    /// <remarks/>
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class ProfileServers
    {

        private ProfileServersVATSIMStatus vATSIMStatusField;

        private ProfileServersGRIB gRIBField;

        private ProfileServersG2G g2GField;

        private ProfileServersSweatBox sweatBoxField;

        private ProfileServersSweatBox2 sweatBox2Field;

        /// <remarks/>
        public ProfileServersVATSIMStatus VATSIMStatus
        {
            get
            {
                return vATSIMStatusField;
            }
            set
            {
                vATSIMStatusField = value;
            }
        }

        /// <remarks/>
        public ProfileServersGRIB GRIB
        {
            get
            {
                return gRIBField;
            }
            set
            {
                gRIBField = value;
            }
        }

        /// <remarks/>
        public ProfileServersG2G G2G
        {
            get
            {
                return g2GField;
            }
            set
            {
                g2GField = value;
            }
        }

        /// <remarks/>
        public ProfileServersSweatBox SweatBox
        {
            get
            {
                return sweatBoxField;
            }
            set
            {
                sweatBoxField = value;
            }
        }

        /// <remarks/>
        public ProfileServersSweatBox2 SweatBox2
        {
            get
            {
                return sweatBox2Field;
            }
            set
            {
                sweatBox2Field = value;
            }
        }
    }

    /// <remarks/>
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class ProfileServersVATSIMStatus
    {

        private string urlField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string url
        {
            get
            {
                return urlField;
            }
            set
            {
                urlField = value;
            }
        }
    }

    /// <remarks/>
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class ProfileServersGRIB
    {

        private string urlField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string url
        {
            get
            {
                return urlField;
            }
            set
            {
                urlField = value;
            }
        }
    }

    /// <remarks/>
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class ProfileServersG2G
    {

        private string urlField;

        private ushort portField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string url
        {
            get
            {
                return urlField;
            }
            set
            {
                urlField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public ushort port
        {
            get
            {
                return portField;
            }
            set
            {
                portField = value;
            }
        }
    }

    /// <remarks/>
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class ProfileServersSweatBox
    {

        private string urlField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string url
        {
            get
            {
                return urlField;
            }
            set
            {
                urlField = value;
            }
        }
    }

    /// <remarks/>
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class ProfileServersSweatBox2
    {

        private string urlField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string url
        {
            get
            {
                return urlField;
            }
            set
            {
                urlField = value;
            }
        }
    }


}
