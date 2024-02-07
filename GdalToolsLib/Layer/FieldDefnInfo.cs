using OSGeo.OGR;
using System;

namespace OGCToolsNetCoreLib.Layer
{
    /// <summary>
    /// wraps some attributes of the ref-type FieldDefn into an own data-class
    /// </summary>
    public class FieldDefnInfo
    {
        /// <summary>
        /// Return the index of the field
        /// </summary>
        public int OgrIndex { get; set; }

        /// <summary>
        /// Return the name of the field
        /// </summary>
        public String Name { get; set; }

        /// <summary>
        /// Returns the name of the type of the field
        /// </summary>
        public String TypeName { get; set; }

        /// <summary>
        /// Returns the name of the domain of the field
        /// </summary>
        public String DomainName { get; set; }


        /// <summary>
        /// returns the type of the field as enum 
        /// </summary>
        public FieldType Type { get; set; }

        /// <summary>
        /// returns the subtype of the field as enum 
        /// </summary>
        public FieldSubType SubType { get; set; }

        /// <summary>
        /// Get the formatting width for this field.
        /// </summary>
        public int? Width { get; set; }

        /// <summary>
        /// Get the formatting precision for this field.
        /// This should normally be zero for fields of types other than OFTReal.
        /// </summary>
        public int? Precision { get; set; }

        /// <summary>
        /// Return whether this field can receive null values.
        /// By default, fields are nullable, so this method is generally called
        /// with FALSE to set a not-null constraint.
        /// Drivers that support writing not-null constraint will advertise the
        /// GDAL_DCAP_NOTNULL_FIELDS driver metadata item.
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Return whether this field has a unique constraint.
        /// By default, fields have no unique constraint.
        /// </summary>
        public bool IsUnique { get; set; }

        /// <summary>
        /// Return whether this field is ignored when fetching features.
        /// </summary>
        public bool IsIgnored { get; set; }

        public FieldDefnInfo(int ogrIndex, FieldDefn field)
        {
            OgrIndex = ogrIndex;
            Name = field.GetName();
            Type = field.GetFieldType();
            TypeName = field.GetFieldTypeName(Type);
            Width = field.GetWidth();
            Precision = field.GetPrecision();
            IsNullable = field.IsNullable() == 1;
            IsUnique = field.IsUnique() == 1;
            DomainName = field.GetDomainName();
            SubType = field.GetSubType();
            IsIgnored = field.IsIgnored() == 1;
        }

        /// <summary>
        /// for test purposes
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="width"></param>
        /// <param name="isNullable"></param>
        /// <param name="isUnique"></param>
        public FieldDefnInfo(string name, FieldType type, int width, bool isNullable, bool isUnique)
        {
            Name = name;
            Type = type;
            Width = width;
            IsNullable = isNullable;
            IsUnique = isUnique;
        }

        public FieldDefn GetFieldDefn()
        {
            FieldType fieldType = Type;
            FieldSubType subType = SubType;

            var fieldDefn = new FieldDefn(Name, Type);
            if (DomainName != null) fieldDefn.SetDomainName(DomainName);
            fieldDefn.SetIgnored(Convert.ToInt32(IsIgnored));
            fieldDefn.SetNullable(Convert.ToInt32(IsNullable));
            fieldDefn.SetUnique(Convert.ToInt32(IsUnique));
            if (Precision != null) fieldDefn.SetPrecision((int)Precision);
            if (Width != null) fieldDefn.SetWidth((int)Width);

            return fieldDefn;
        }

        ///// <summary>
        ///// for test purpose
        ///// </summary>
        ///// <returns></returns>
        //public static FieldDefnInfo CreateTest(string name, FieldType type, int width)
        //{
        //    return new FieldDefnInfo(name, type, width);
        //}

        public override string ToString()
        {
            return $"Field {Name}, Type {Type}, Width {Width}, Null {IsNullable}, Unique {IsUnique}";
        }
    }
}
