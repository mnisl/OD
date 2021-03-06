//This file is automatically generated.
//Do not attempt to make changes to this file because the changes will be erased and overwritten.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;

namespace OpenDentBusiness.Crud{
	public class WikiListHistCrud {
		///<summary>Gets one WikiListHist object from the database using the primary key.  Returns null if not found.</summary>
		public static WikiListHist SelectOne(long wikiListHistNum){
			string command="SELECT * FROM wikilisthist "
				+"WHERE WikiListHistNum = "+POut.Long(wikiListHistNum);
			List<WikiListHist> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets one WikiListHist object from the database using a query.</summary>
		public static WikiListHist SelectOne(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<WikiListHist> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets a list of WikiListHist objects from the database using a query.</summary>
		public static List<WikiListHist> SelectMany(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<WikiListHist> list=TableToList(Db.GetTable(command));
			return list;
		}

		///<summary>Converts a DataTable to a list of objects.</summary>
		public static List<WikiListHist> TableToList(DataTable table){
			List<WikiListHist> retVal=new List<WikiListHist>();
			WikiListHist wikiListHist;
			for(int i=0;i<table.Rows.Count;i++) {
				wikiListHist=new WikiListHist();
				wikiListHist.WikiListHistNum= PIn.Long  (table.Rows[i]["WikiListHistNum"].ToString());
				wikiListHist.UserNum        = PIn.Long  (table.Rows[i]["UserNum"].ToString());
				wikiListHist.ListName       = PIn.String(table.Rows[i]["ListName"].ToString());
				wikiListHist.ListHeaders    = PIn.String(table.Rows[i]["ListHeaders"].ToString());
				wikiListHist.ListContent    = PIn.String(table.Rows[i]["ListContent"].ToString());
				wikiListHist.DateTimeSaved  = PIn.DateT (table.Rows[i]["DateTimeSaved"].ToString());
				retVal.Add(wikiListHist);
			}
			return retVal;
		}

		///<summary>Inserts one WikiListHist into the database.  Returns the new priKey.</summary>
		public static long Insert(WikiListHist wikiListHist){
			if(DataConnection.DBtype==DatabaseType.Oracle) {
				wikiListHist.WikiListHistNum=DbHelper.GetNextOracleKey("wikilisthist","WikiListHistNum");
				int loopcount=0;
				while(loopcount<100){
					try {
						return Insert(wikiListHist,true);
					}
					catch(Oracle.DataAccess.Client.OracleException ex){
						if(ex.Number==1 && ex.Message.ToLower().Contains("unique constraint") && ex.Message.ToLower().Contains("violated")){
							wikiListHist.WikiListHistNum++;
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
				return Insert(wikiListHist,false);
			}
		}

		///<summary>Inserts one WikiListHist into the database.  Provides option to use the existing priKey.</summary>
		public static long Insert(WikiListHist wikiListHist,bool useExistingPK){
			if(!useExistingPK && PrefC.RandomKeys) {
				wikiListHist.WikiListHistNum=ReplicationServers.GetKey("wikilisthist","WikiListHistNum");
			}
			string command="INSERT INTO wikilisthist (";
			if(useExistingPK || PrefC.RandomKeys) {
				command+="WikiListHistNum,";
			}
			command+="UserNum,ListName,ListHeaders,ListContent,DateTimeSaved) VALUES(";
			if(useExistingPK || PrefC.RandomKeys) {
				command+=POut.Long(wikiListHist.WikiListHistNum)+",";
			}
			command+=
				     POut.Long  (wikiListHist.UserNum)+","
				+"'"+POut.String(wikiListHist.ListName)+"',"
				+"'"+POut.String(wikiListHist.ListHeaders)+"',"
				+"'"+POut.String(wikiListHist.ListContent)+"',"
				+    POut.DateT (wikiListHist.DateTimeSaved)+")";
			if(useExistingPK || PrefC.RandomKeys) {
				Db.NonQ(command);
			}
			else {
				wikiListHist.WikiListHistNum=Db.NonQ(command,true);
			}
			return wikiListHist.WikiListHistNum;
		}

		///<summary>Updates one WikiListHist in the database.</summary>
		public static void Update(WikiListHist wikiListHist){
			string command="UPDATE wikilisthist SET "
				+"UserNum        =  "+POut.Long  (wikiListHist.UserNum)+", "
				+"ListName       = '"+POut.String(wikiListHist.ListName)+"', "
				+"ListHeaders    = '"+POut.String(wikiListHist.ListHeaders)+"', "
				+"ListContent    = '"+POut.String(wikiListHist.ListContent)+"', "
				+"DateTimeSaved  =  "+POut.DateT (wikiListHist.DateTimeSaved)+" "
				+"WHERE WikiListHistNum = "+POut.Long(wikiListHist.WikiListHistNum);
			Db.NonQ(command);
		}

		///<summary>Updates one WikiListHist in the database.  Uses an old object to compare to, and only alters changed fields.  This prevents collisions and concurrency problems in heavily used tables.  Returns true if an update occurred.</summary>
		public static bool Update(WikiListHist wikiListHist,WikiListHist oldWikiListHist){
			string command="";
			if(wikiListHist.UserNum != oldWikiListHist.UserNum) {
				if(command!=""){ command+=",";}
				command+="UserNum = "+POut.Long(wikiListHist.UserNum)+"";
			}
			if(wikiListHist.ListName != oldWikiListHist.ListName) {
				if(command!=""){ command+=",";}
				command+="ListName = '"+POut.String(wikiListHist.ListName)+"'";
			}
			if(wikiListHist.ListHeaders != oldWikiListHist.ListHeaders) {
				if(command!=""){ command+=",";}
				command+="ListHeaders = '"+POut.String(wikiListHist.ListHeaders)+"'";
			}
			if(wikiListHist.ListContent != oldWikiListHist.ListContent) {
				if(command!=""){ command+=",";}
				command+="ListContent = '"+POut.String(wikiListHist.ListContent)+"'";
			}
			if(wikiListHist.DateTimeSaved != oldWikiListHist.DateTimeSaved) {
				if(command!=""){ command+=",";}
				command+="DateTimeSaved = "+POut.DateT(wikiListHist.DateTimeSaved)+"";
			}
			if(command==""){
				return false;
			}
			command="UPDATE wikilisthist SET "+command
				+" WHERE WikiListHistNum = "+POut.Long(wikiListHist.WikiListHistNum);
			Db.NonQ(command);
			return true;
		}

		///<summary>Deletes one WikiListHist from the database.</summary>
		public static void Delete(long wikiListHistNum){
			string command="DELETE FROM wikilisthist "
				+"WHERE WikiListHistNum = "+POut.Long(wikiListHistNum);
			Db.NonQ(command);
		}

	}
}