using UnityEngine;
using System.Collections;
using Assets.Scripts;
using System;
using System.Collections.Generic;

public class Info
{
    [PrimaryKeyField()]
    public int Id { get; set; }

    public string Name { get; set; }

    public int Score { get; set; }

    public bool isMan { get; set; }

    public DateTime date { get; set; }
}

public class Info2
{
    [PrimaryKeyField()]
    public int Id { get; set; }

    [ForeignKeyField("Info", "Id", ForeignKeyAction.NoAction, ForeignKeyAction.Cascade)]
    public int ForeignId { get; set; }
}

public class Info3
{
    [PrimaryKeyField()]
    public int Id { get; set; }

    [ForeignKeyField("Info", "Id", ForeignKeyAction.SetDefault, ForeignKeyAction.SetDefault)]
    [Default("1")]
    public int ForeignId { get; set; }
}

public class Info4
{
    [PrimaryKeyField()]
    public int Id { get; set; }

    [Default("")]
    public string ForeignId { get; set; }
}



public class Test : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
        using(DatabaseManager manager = new DatabaseManager("test.db"))
        {
            if (manager.ConnectToDatabase())
            {
                /*
                //when first time Starting your script
                //you need to add your table into db
                manager.CreateTable<Info>();

                //if you need to add new record 
                manager.InsertRecord<Info>(new Info() {  date = DateTime.Now, isMan = true, Name = "Alexander", Score = 0 });
                manager.InsertRecord<Info>(new Info() { date = DateTime.Now, isMan = false, Name = "Mariya", Score = 0 });

                //to read all data and write it into List
                List<Info> allPers = (List<Info>)manager.ReadAll<Info>();

                foreach(var pers in allPers)
                {
                    print(" Name: " + pers.Name + " Man: " + pers.isMan + " Score: " + pers.Score + " Date of registration: " + pers.date + "\n");
                }

                //foreign key examples
                manager.CreateTable<Info2>();
                manager.CreateTable<Info3>();
                manager.CreateTable<Info4>();
                 * */
            }
        }
	}
}
