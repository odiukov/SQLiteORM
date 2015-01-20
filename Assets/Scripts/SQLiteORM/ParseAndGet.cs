using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Assets.Scripts
{
    public class ParseAndGet
    {
        private string header_text = "";
        private string body_text = "";
        private string footer_text = "";
        private Dictionary<string, string> types = new Dictionary<string, string>();
        public ParseAndGet()
        {
            types.Add("Int32", "INTEGER");
            types.Add("String", "TEXT");
            types.Add("DateTime", "TEXT");
            types.Add("Single", "FLOAT");
            types.Add("Double", "DOUBLE");
            types.Add("Boolean", "BOOLEAN");
        }
        private PropertyInfo[] GetProperties<T>(T item)
        {
            return typeof(T).GetProperties();
        }
        public string CreateInstruction<T>() where T : class
        {
            string foreign_keys = "";
            var f = typeof(T).GetProperties();
            header_text += "CREATE TABLE " + (typeof(T)).Name + " (";
            foreach (var it in f)
            {
                body_text += string.Format("{0} {1} ", it.Name, types[it.PropertyType.Name]);
                var attr = it.GetCustomAttributes(false);
                foreach (var i in attr)
                {
                    if (i is PrimaryKeyField)
                        body_text +=  "PRIMARY KEY NOT NULL ";
                    if (i is ForeignKeyField)
                        foreign_keys += String.Format("FOREIGN KEY ({0}) REFERENCES {1}({2}) ON DELETE {3} ON UPDATE {4},\n",
                            it.Name,
                            ((ForeignKeyField)i).foreignTableName,
                            ((ForeignKeyField)i).foreignFieldName,
                            actoinConverter[((ForeignKeyField)i).onDelete], 
                            actoinConverter[((ForeignKeyField)i).onUpdate]);
                    if ((i is Default) && types[it.PropertyType.Name] != "TEXT")
                        body_text += "DEFAULT " + ((Default)i).defValue;
                    else if ((i is Default) && types[it.PropertyType.Name] == "TEXT")
                        body_text += "DEFAULT " + "'" + ((Default)i).defValue + "'";
                }
                body_text += ",\n";
            }
            body_text += foreign_keys;
            footer_text = ");";
            return string.Format("{0}\n{1}{2}", header_text, body_text.Substring(0, body_text.Length - 2), footer_text);
        }
        public string InsertInstruction<T>(T item) where T : class
        {
            header_text = "";
            body_text = "";
            footer_text = "";
            var props = typeof(T).GetProperties();
            header_text += "INSERT INTO " + (typeof(T)).Name + " VALUES (";

            foreach (var it in props)
                switch ((it.GetValue(item, null)).GetType().Name)
                {
                    case "Boolean":
                        body_text += string.Format("{0}, ", (it.GetValue(item, null).ToString() == "False") ? 0 : 1);
                        break;

                    case "Int32":
                        var attr = it.GetCustomAttributes(false);
                        if (attr != null && attr.Count() > 0)
                        {
                            foreach (var i in attr)
                            {
                                if (i is PrimaryKeyField)
                                    body_text += string.Format("{0}, ", "null");
                                else
                                {
                                     body_text += string.Format("{0}, ", it.GetValue(item, null));
                                }
                                //if(i is ForeignKeyField)
                            }
                        }
                        else
                            body_text += string.Format("{0}, ", it.GetValue(item, null));
                        break;

                    case "DateTime":
                    case "String":
                        body_text += string.Format("'{0}', ", it.GetValue(item, null));
                        break;

                    default:
                        body_text += string.Format("{0}, ", it.GetValue(item, null));
                        break;

                }

            footer_text = ");";
            return string.Format("{0}{1}{2}", header_text, body_text.Substring(0, body_text.Length - 2), footer_text); 
        }
        public IEnumerable<string> InsertCollectionInstruction<T>(IEnumerable<T> collection) where T : class
        {
            List<string> instructions = new List<string>();
            header_text = "";
            body_text = "";
            footer_text = "";
            var props = typeof(T).GetProperties();
            header_text += "INSERT INTO " + (typeof(T)).Name + " VALUES (";

            foreach (var item in collection)
            {
                foreach (var it in props)
                    if ((it.GetValue(item, null)).GetType().Name == "String" || (it.GetValue(item, null)).GetType().Name == "DateTime")
                        body_text += string.Format("'{0}', ", it.GetValue(item, null));
                    else
                        body_text += string.Format("{0}, ", it.GetValue(item, null));
                footer_text = ");";
                instructions.Add(string.Format("{0}{1}{2}", header_text, body_text.Substring(0, body_text.Length - 2), footer_text));
                body_text = "";
            }
            return instructions;
        }
        public string RemoveItemsById<T>(object value)
        {
            if (value.GetType().Name == "String" || value.GetType().Name == "DateTime")
                return "DELETE FROM " + typeof(T).Name + " WHERE Id  = '" + value + "'";
            else
                return "DELETE FROM " + typeof(T).Name + " WHERE Id  = " + value;
        }
        public string UpdateItemsByFieldName<T>(string fieldNameSearch, string valueSearch, string fieldNameToUpdate, string newValue)
        {
            return "UPDATE " + typeof(T).Name + " SET " + fieldNameToUpdate + " = " + newValue + " WHERE " + fieldNameSearch + " = " + valueSearch;
        }

        //
        public string UpdateRecord<T>(string fieldNameSearch, string valueSearch, T newItem)
        {
            header_text = "";
            body_text = "";
            footer_text = "";
            header_text = "UPDATE " + typeof(T).Name + " SET ";            
            var props = typeof(T).GetProperties();
            foreach (var it in props)
            {
                switch ((it.GetValue(newItem, null)).GetType().Name)
                {
                    case "Boolean":
                        body_text += it.Name + string.Format("{0}, ", (it.GetValue(newItem, null).ToString() == "False") ? 0 : 1);
                        break;

                    case "Int32":
                        var attr = it.GetCustomAttributes(false);
                        if (attr != null && attr.Count() > 0)
                        {
                            foreach (var i in attr)
                            {
                                if (i is ForeignKeyField)
                                    body_text += it.Name + " = " + it.GetValue(newItem, null) + ", ";
                                if((i is Default))
                                    body_text += it.Name + " = " + it.GetValue(newItem, null) + ", ";
                            }
                        }
                        else
                            body_text += it.Name + " = " + it.GetValue(newItem, null) + ", ";    
                        break;

                    case "DateTime":
                    case "String":
                        body_text += it.Name + " = '" + it.GetValue(newItem, null) + "', "; 
                        break;
                    
                    default:
                        body_text += it.Name + " = " + it.GetValue(newItem, null) + ", ";    
                        break;

                }
            }
            footer_text = " WHERE " + fieldNameSearch + " = " + valueSearch;
            return string.Format("{0}{1}{2}", header_text, body_text.Substring(0, body_text.Length - 2), footer_text); 
        }
        //
        public string ReadAllInstruction<T>()
        {
            return "SELECT * FROM " + typeof(T).Name;
        }
        public string DropTableInstruction<T>()
        {
            return "DROP TABLE " + typeof(T).Name;
        }

        private Dictionary<ForeignKeyAction, string> actoinConverter = new Dictionary<ForeignKeyAction, string>()
        {
            {ForeignKeyAction.NoAction, "NO ACTION"},
            {ForeignKeyAction.Restrict, "RESTRICT"},
            {ForeignKeyAction.SetNull, "SET NULL"},
            {ForeignKeyAction.SetDefault, "SET DEFAULT"},
            {ForeignKeyAction.Cascade, "CASCADE"}
        };
    }
    public class PrimaryKeyField : System.Attribute
    {
        public PrimaryKeyField()
        {
        }
    }

    public class Default : System.Attribute
    {
        public string defValue;
        public Default(string value)
        {
            defValue = value;
        }
    }
    public class ForeignKeyField : System.Attribute
    {
        public ForeignKeyAction onDelete;
        public ForeignKeyAction onUpdate;
        public string foreignTableName { get; set; }
        public string foreignFieldName { get; set; }
        public ForeignKeyField(string TabName, string FieldName)
        {
            foreignTableName = TabName;
            foreignFieldName = FieldName;
            onDelete = ForeignKeyAction.NoAction;
            onUpdate = ForeignKeyAction.NoAction;
        }

        public ForeignKeyField(string TabName, string FieldName, ForeignKeyAction onDel, ForeignKeyAction onUpd)
        {
            foreignTableName = TabName;
            foreignFieldName = FieldName;
            onDelete = onDel;
            onUpdate = onUpd;
        }
    }
    public enum ForeignKeyAction
    {
        NoAction,
        Restrict,
        SetNull,
        SetDefault,
        Cascade
    };

    
}
