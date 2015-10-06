//This file is automatically generated.
//Do not attempt to make changes to this file because the changes will be erased and overwritten.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;

namespace OpenDentBusiness.Crud{
	public class LetterCrud {
		///<summary>Gets one Letter object from the database using the primary key.  Returns null if not found.</summary>
		public static Letter SelectOne(long letterNum){
			string command="SELECT * FROM letter "
				+"WHERE LetterNum = "+POut.Long(letterNum);
			List<Letter> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets one Letter object from the database using a query.</summary>
		public static Letter SelectOne(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<Letter> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets a list of Letter objects from the database using a query.</summary>
		public static List<Letter> SelectMany(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<Letter> list=TableToList(Db.GetTable(command));
			return list;
		}

		///<summary>Converts a DataTable to a list of objects.</summary>
		public static List<Letter> TableToList(DataTable table){
			List<Letter> retVal=new List<Letter>();
			Letter letter;
			for(int i=0;i<table.Rows.Count;i++) {
				letter=new Letter();
				letter.LetterNum  = PIn.Long  (table.Rows[i]["LetterNum"].ToString());
				letter.Description= PIn.String(table.Rows[i]["Description"].ToString());
				letter.BodyText   = PIn.String(table.Rows[i]["BodyText"].ToString());
				retVal.Add(letter);
			}
			return retVal;
		}

		///<summary>Inserts one Letter into the database.  Returns the new priKey.</summary>
		public static long Insert(Letter letter){
			if(DataConnection.DBtype==DatabaseType.Oracle) {
				letter.LetterNum=DbHelper.GetNextOracleKey("letter","LetterNum");
				int loopcount=0;
				while(loopcount<100){
					try {
						return Insert(letter,true);
					}
					catch(Oracle.DataAccess.Client.OracleException ex){
						if(ex.Number==1 && ex.Message.ToLower().Contains("unique constraint") && ex.Message.ToLower().Contains("violated")){
							letter.LetterNum++;
							loopcount++;
						}
						else{
							throw ex;
						}
					}
				}
				throw new ApplicationException("Insert failed.  Could not generate primary key.");
			}
			else {
				return Insert(letter,false);
			}
		}

		///<summary>Inserts one Letter into the database.  Provides option to use the existing priKey.</summary>
		public static long Insert(Letter letter,bool useExistingPK){
			if(!useExistingPK && PrefC.RandomKeys) {
				letter.LetterNum=ReplicationServers.GetKey("letter","LetterNum");
			}
			string command="INSERT INTO letter (";
			if(useExistingPK || PrefC.RandomKeys) {
				command+="LetterNum,";
			}
			command+="Description,BodyText) VALUES(";
			if(useExistingPK || PrefC.RandomKeys) {
				command+=POut.Long(letter.LetterNum)+",";
			}
			command+=
				 "'"+POut.String(letter.Description)+"',"
				+    DbHelper.ParamChar+"paramBodyText)";
			if(letter.BodyText==null) {
				letter.BodyText="";
			}
			OdSqlParameter paramBodyText=new OdSqlParameter("paramBodyText",OdDbType.Text,letter.BodyText);
			if(useExistingPK || PrefC.RandomKeys) {
				Db.NonQ(command,paramBodyText);
			}
			else {
				letter.LetterNum=Db.NonQ(command,true,paramBodyText);
			}
			return letter.LetterNum;
		}

		///<summary>Updates one Letter in the database.</summary>
		public static void Update(Letter letter){
			string command="UPDATE letter SET "
				+"Description= '"+POut.String(letter.Description)+"', "
				+"BodyText   =  "+DbHelper.ParamChar+"paramBodyText "
				+"WHERE LetterNum = "+POut.Long(letter.LetterNum);
			if(letter.BodyText==null) {
				letter.BodyText="";
			}
			OdSqlParameter paramBodyText=new OdSqlParameter("paramBodyText",OdDbType.Text,letter.BodyText);
			Db.NonQ(command,paramBodyText);
		}

		///<summary>Updates one Letter in the database.  Uses an old object to compare to, and only alters changed fields.  This prevents collisions and concurrency problems in heavily used tables.  Returns true if an update occurred.</summary>
		public static bool Update(Letter letter,Letter oldLetter){
			string command="";
			if(letter.Description != oldLetter.Description) {
				if(command!=""){ command+=",";}
				command+="Description = '"+POut.String(letter.Description)+"'";
			}
			if(letter.BodyText != oldLetter.BodyText) {
				if(command!=""){ command+=",";}
				command+="BodyText = "+DbHelper.ParamChar+"paramBodyText";
			}
			if(command==""){
				return false;
			}
			if(letter.BodyText==null) {
				letter.BodyText="";
			}
			OdSqlParameter paramBodyText=new OdSqlParameter("paramBodyText",OdDbType.Text,letter.BodyText);
			command="UPDATE letter SET "+command
				+" WHERE LetterNum = "+POut.Long(letter.LetterNum);
			Db.NonQ(command,paramBodyText);
			return true;
		}

		///<summary>Deletes one Letter from the database.</summary>
		public static void Delete(long letterNum){
			string command="DELETE FROM letter "
				+"WHERE LetterNum = "+POut.Long(letterNum);
			Db.NonQ(command);
		}

	}
}