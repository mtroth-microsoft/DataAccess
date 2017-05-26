﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.5472
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Xml.Serialization;
    using System.Collections.Generic;

    // 
    // This source code was auto-generated by xsd, Version=2.0.50727.42.
    // 


    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    [System.Xml.Serialization.XmlRootAttribute("InfrastructureConfig", Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store", IsNullable = false)]
    public partial class InfrastructureConfigType
    {

        private AccessConfigurationType accessField;

        private PagingConfigurationType pagingSizesField;

        private DataFilterConfigurationType dataFiltersField;

        private ObsoletedConfigurationType obsoletedField;

        private GroupByConfigurationType groupByField;

        /// <remarks/>
        public AccessConfigurationType Access
        {
            get
            {
                return this.accessField;
            }
            set
            {
                this.accessField = value;
            }
        }

        /// <remarks/>
        public PagingConfigurationType PagingSizes
        {
            get
            {
                return this.pagingSizesField;
            }
            set
            {
                this.pagingSizesField = value;
            }
        }

        /// <remarks/>
        public DataFilterConfigurationType DataFilters
        {
            get
            {
                return this.dataFiltersField;
            }
            set
            {
                this.dataFiltersField = value;
            }
        }

        /// <remarks/>
        public ObsoletedConfigurationType Obsoleted
        {
            get
            {
                return this.obsoletedField;
            }
            set
            {
                this.obsoletedField = value;
            }
        }

        /// <remarks/>
        public GroupByConfigurationType GroupBy
        {
            get
            {
                return this.groupByField;
            }
            set
            {
                this.groupByField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public partial class DataFilterConfigurationType
    {
        private List<DataFilterConfigurationItemType> entitySetsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("EntitySet", typeof(EntitySetDataFilterType))]
        [System.Xml.Serialization.XmlElementAttribute("Operation", typeof(OperationDataFilterType))]
        public List<DataFilterConfigurationItemType> EdmElements
        {
            get
            {
                return this.entitySetsField;
            }
            set
            {
                this.entitySetsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public partial class PagingConfigurationType
    {
        private List<PagingConfigurationItemType> entitySetsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("EntitySet")]
        public List<PagingConfigurationItemType> EntitySets
        {
            get
            {
                return this.entitySetsField;
            }
            set
            {
                this.entitySetsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public partial class AccessConfigurationType
    {

        private List<ServiceOperationAccessType> serviceOperationsField;

        private List<EntitySetAccessType> entitySetsField;

        private List<ServiceActionAccessType> serviceActionsField;

        private bool writeEnabledField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("ServiceOperation", IsNullable = false)]
        public List<ServiceOperationAccessType> ServiceOperations
        {
            get
            {
                return this.serviceOperationsField;
            }
            set
            {
                this.serviceOperationsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("EntitySet", IsNullable = false)]
        public List<EntitySetAccessType> EntitySets
        {
            get
            {
                return this.entitySetsField;
            }
            set
            {
                this.entitySetsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("ServiceAction", IsNullable = false)]
        public List<ServiceActionAccessType> ServiceActions
        {
            get
            {
                return this.serviceActionsField;
            }
            set
            {
                this.serviceActionsField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("WriteEnabled")]
        public bool WriteEnabled
        {
            get
            {
                return this.writeEnabledField;
            }
            set
            {
                this.writeEnabledField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public partial class DataFilterParameterType
    {
        private string nameField;

        private DataType dataTypeField;

        private bool requiredField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "token")]
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public DataType DataType
        {
            get
            {
                return this.dataTypeField;
            }
            set
            {
                this.dataTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool Required
        {
            get
            {
                return this.requiredField;
            }
            set
            {
                this.requiredField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public partial class ServiceActionAccessType : ConfigurationItemType
    {

        private ServiceActionAccessibility accessField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ServiceActionAccessibility Access
        {
            get
            {
                return this.accessField;
            }
            set
            {
                this.accessField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public partial class ServiceOperationAccessType : ConfigurationItemType
    {

        private ServiceOperationAccessibility accessField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ServiceOperationAccessibility Access
        {
            get
            {
                return this.accessField;
            }
            set
            {
                this.accessField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public enum ServiceActionAccessibility
    {

        /// <remarks/>
        None,

        /// <remarks/>
        Invoke,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public enum ServiceOperationAccessibility
    {

        /// <remarks/>
        All,

        /// <remarks/>
        AllRead,

        /// <remarks/>
        None,

        /// <remarks/>
        OverrideEntitySetRights,

        /// <remarks/>
        ReadMultiple,

        /// <remarks/>
        ReadSingle,
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(EntitySetGroupByType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationGroupByType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(GroupByConfigurationItemType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(EntitySetObsoletedType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationObsoletedType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(ObsoletedConfigurationItemType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(EntitySetDataFilterType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationDataFilterType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(PagingConfigurationItemType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(EntitySetAccessType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(ServiceOperationAccessType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(ServiceActionAccessType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(DataFilterConfigurationItemType))]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public abstract partial class ConfigurationItemType
    {

        private string nameField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public partial class PagingConfigurationItemType : ConfigurationItemType
    {

        private int sizeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int Size
        {
            get
            {
                return this.sizeField;
            }
            set
            {
                this.sizeField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public partial class EntitySetDataFilterType : DataFilterConfigurationItemType
    {
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public partial class OperationDataFilterType : DataFilterConfigurationItemType
    {
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(EntitySetDataFilterType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationDataFilterType))]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public abstract partial class DataFilterConfigurationItemType : ConfigurationItemType
    {
        private List<DataFilterParameterType> parametersField;

        private string targetEntitySet;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string TargetEntitySet
        {
            get
            {
                return this.targetEntitySet;
            }
            set
            {
                this.targetEntitySet = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Parameter")]
        public List<DataFilterParameterType> Parameters
        {
            get
            {
                return this.parametersField;
            }
            set
            {
                this.parametersField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public partial class EntitySetAccessType : ConfigurationItemType
    {

        private EntitySetAccessibility accessField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public EntitySetAccessibility Access
        {
            get
            {
                return this.accessField;
            }
            set
            {
                this.accessField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(EntitySetObsoletedType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationObsoletedType))]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public abstract partial class ObsoletedConfigurationItemType : ConfigurationItemType
    {
        private System.DateTime removalEstimateField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public System.DateTime RemovalEstimate
        {
            get
            {
                return this.removalEstimateField;
            }
            set
            {
                this.removalEstimateField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public partial class EntitySetObsoletedType : ObsoletedConfigurationItemType
    {
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public partial class OperationObsoletedType : ObsoletedConfigurationItemType
    {
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public partial class ObsoletedConfigurationType
    {
        private List<ObsoletedConfigurationItemType> obsoletedField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("EntitySet", typeof(EntitySetObsoletedType))]
        [System.Xml.Serialization.XmlElementAttribute("Operation", typeof(OperationObsoletedType))]
        public List<ObsoletedConfigurationItemType> Apis
        {
            get
            {
                return this.obsoletedField;
            }
            set
            {
                this.obsoletedField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public partial class GroupByItemType
    {
        private string nameField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "token")]
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationGroupByType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(EntitySetGroupByType))]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public abstract partial class GroupByConfigurationItemType : ConfigurationItemType
    {
        private List<GroupByItemType> itemsField = new List<GroupByItemType>();

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Item")]
        public List<GroupByItemType> Items
        {
            get
            {
                return this.itemsField;
            }
            set
            {
                this.itemsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public partial class OperationGroupByType : GroupByConfigurationItemType
    {
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public partial class EntitySetGroupByType : GroupByConfigurationItemType
    {
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public partial class GroupByConfigurationType
    {
        private List<GroupByConfigurationItemType> groupByField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Operation", typeof(OperationGroupByType))]
        [System.Xml.Serialization.XmlElementAttribute("EntitySet", typeof(EntitySetGroupByType))]
        public List<GroupByConfigurationItemType> Apis
        {
            get
            {
                return this.groupByField;
            }
            set
            {
                this.groupByField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public enum EntitySetAccessibility
    {

        /// <remarks/>
        All,

        /// <remarks/>
        AllRead,

        /// <remarks/>
        AllWrite,

        /// <remarks/>
        None,

        /// <remarks/>
        ReadMultiple,

        /// <remarks/>
        ReadSingle,

        /// <remarks/>
        WriteAppend,

        /// <remarks/>
        WriteDelete,

        /// <remarks/>
        WriteMerge,

        /// <remarks/>
        WriteReplace,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/services/telemetry/configuration/2013/store")]
    public enum DataType
    {

        /// <remarks/>
        @string,

        /// <remarks/>
        @int,

        /// <remarks/>
        @bool,

        /// <remarks/>
        @long,

        /// <remarks/>
        datetimeoffset,
    }
}