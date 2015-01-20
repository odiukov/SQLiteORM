using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Data;
using Mono.Data.Sqlite;
using System.Data;
using UnityEngine;
using System.IO;

namespace Assets.Scripts
{

    public class DatabaseManager : IDisposable
    {
        private string _connectionString = "";
        private IDbConnection _connection = null;
        private IDbCommand _command = null;
        private IDataReader _reader = null;

        public DatabaseManager(string dataBaseName)
        {
#if UNITY_ANDROID && !UNITY_EDITOR

            string filepath = Application.persistentDataPath + "/" + dataBaseName; 
            if(!File.Exists(filepath)) 
            { 
                WWW loadDB = new WWW(String.Format("jar:file://{0}!/assets/{1}", Application.dataPath, dataBaseName));  // this is the path to your StreamingAssets in android
                while(!loadDB.isDone) {}  // CAREFUL here, for safety reasons you shouldn't let this while loop unattended, place a timer and error check
                // then save to Application.persistentDataPath
                File.WriteAllBytes(filepath, loadDB.bytes); 
            }       
            _connectionString = "URI=file:" + filepath;
#endif
#if UNITY_EDITOR
            _connectionString = String.Format("URI=file:{0}/StreamingAssets/{1}", Application.dataPath, dataBaseName);
#endif
        }

        public DatabaseManager() : this("") { }

        public bool ConnectToDatabase()
        {
            _connection = (IDbConnection)new SqliteConnection(_connectionString);

            _connection.Open();

            if (_connection.State == System.Data.ConnectionState.Open)
            {
                //_command.
                _command = _connection.CreateCommand();
                return true;
            }
            return false;
        }

        public bool ExecuteScript(string script)
        {
            try
            {
                _command.CommandText = "PRAGMA foreign_keys = ON";
                var id1 = _command.ExecuteNonQuery();
                _command.CommandText = script;
                var id2 = _command.ExecuteNonQuery();
                return true;
            }
            catch (SqliteException ex)
            {
                Debug.Log(ex.ToString());
                return false;
            }

        }

        private T CreateObject<T>()
        {
            return (T)Activator.CreateInstance(typeof(T)); ;
        }

        public IEnumerable<T> ReadAll<T>() where T : class
        {
            List<T> result = new List<T>();
            //   T obj = (T)Activator.CreateInstance(typeof(T)); ; // your object

            string sql = "SELECT * FROM " + typeof(T).Name;
            _command.CommandText = sql;
            _reader = _command.ExecuteReader();
            var props = typeof(T).GetProperties();
            while (_reader.Read())
            {
                var obj = CreateObject<T>();
                for (int i = 0; i < props.Length; i++)
                {
                    props[i].SetValue(obj, Convert.ChangeType(_reader.GetValue(i), props[i].PropertyType), null);
                }
                result.Add(obj);
            }
            _reader.Close();
            _reader = null;
            return result;
        }


       public IEnumerable<T> ReadByFieldName<T>(string fieldName, object value) where T : class
        {

            var valueType = value.GetType().Name;
            string sql = "";
            List<T> result = new List<T>();
            if (valueType == "String")
                sql = "SELECT * FROM " + typeof(T).Name + " WHERE " + fieldName + " = '" + value + "'";
            else
                sql = "SELECT * FROM " + typeof(T).Name + " WHERE " + fieldName + " = " + value;
            _command.CommandText = sql;
            _reader = _command.ExecuteReader();
            var props = typeof(T).GetProperties();
            while (_reader.Read())
            {
                var obj = CreateObject<T>();
                for (int i = 0; i < props.Length; i++)
                {
                    props[i].SetValue(obj, Convert.ChangeType(_reader.GetValue(i), props[i].PropertyType), null);
                }
                result.Add(obj);
            }
            _reader.Close();
            _reader = null;
            return result;
        }

        public bool CreateTable<T>() where T : class
        {
            string tabInstruction = (new ParseAndGet()).CreateInstruction<T>();
            if (ExecuteScript(tabInstruction))
                return true;
            return false;
        }

        public void InsertRecord<T>(T item) where T : class
        {

            string tabInstruction = (new ParseAndGet()).InsertInstruction<T>(item);
            ExecuteScript(tabInstruction);
        }

        public bool UpdateFieldInRecord<T>(string fieldNameSearch, string valueSearch, string fieldNameToUpdate, string newValue)
        {
            if (ExecuteScript(new ParseAndGet().UpdateItemsByFieldName<T>(fieldNameSearch, valueSearch, fieldNameToUpdate, newValue)))
                return true;
            return false;
        }

        public bool UpdateRecordByFieldName<T>(string fieldNameSearch, string valueSearch, T newRecord)
        {
            if (ExecuteScript(new ParseAndGet().UpdateRecord<T>(fieldNameSearch, valueSearch, newRecord)))
                return true;
            return false;
        }

        public bool DeleteRecordById<T>(object id)
        {
            if (ExecuteScript(new ParseAndGet().RemoveItemsById<T>(id)))
                return true;
            return false;
        }

        public bool DropTable<T>()
        {
            if (ExecuteScript(new ParseAndGet().DropTableInstruction<T>()))
                return true;
            return false;
        }

        public void Dispose()
        {

            _connection.Close();
            _connection = null;
            _command.Dispose();
            _command = null;

        }
    }
}
