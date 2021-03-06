//This file is automatically generated.
//Do not attempt to make changes to this file because the changes will be erased and overwritten.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;

namespace OpenDentBusiness.Crud{
	public class DiseaseCrud {
		///<summary>Gets one Disease object from the database using the primary key.  Returns null if not found.</summary>
		public static Disease SelectOne(long diseaseNum){
			string command="SELECT * FROM disease "
				+"WHERE DiseaseNum = "+POut.Long(diseaseNum);
			List<Disease> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets one Disease object from the database using a query.</summary>
		public static Disease SelectOne(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<Disease> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets a list of Disease objects from the database using a query.</summary>
		public static List<Disease> SelectMany(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<Disease> list=TableToList(Db.GetTable(command));
			return list;
		}

		///<summary>Converts a DataTable to a list of objects.</summary>
		public static List<Disease> TableToList(DataTable table){
			List<Disease> retVal=new List<Disease>();
			Disease disease;
			for(int i=0;i<table.Rows.Count;i++) {
				disease=new Disease();
				disease.DiseaseNum       = PIn.Long  (table.Rows[i]["DiseaseNum"].ToString());
				disease.PatNum           = PIn.Long  (table.Rows[i]["PatNum"].ToString());
				disease.DiseaseDefNum    = PIn.Long  (table.Rows[i]["DiseaseDefNum"].ToString());
				disease.PatNote          = PIn.String(table.Rows[i]["PatNote"].ToString());
				disease.DateTStamp       = PIn.DateT (table.Rows[i]["DateTStamp"].ToString());
				disease.ProbStatus       = (OpenDentBusiness.ProblemStatus)PIn.Int(table.Rows[i]["ProbStatus"].ToString());
				disease.DateStart        = PIn.Date  (table.Rows[i]["DateStart"].ToString());
				disease.DateStop         = PIn.Date  (table.Rows[i]["DateStop"].ToString());
				disease.SnomedProblemType= PIn.String(table.Rows[i]["SnomedProblemType"].ToString());
				disease.FunctionStatus   = (OpenDentBusiness.FunctionalStatus)PIn.Int(table.Rows[i]["FunctionStatus"].ToString());
				retVal.Add(disease);
			}
			return retVal;
		}

		///<summary>Inserts one Disease into the database.  Returns the new priKey.</summary>
		public static long Insert(Disease disease){
			if(DataConnection.DBtype==DatabaseType.Oracle) {
				disease.DiseaseNum=DbHelper.GetNextOracleKey("disease","DiseaseNum");
				int loopcount=0;
				while(loopcount<100){
					try {
						return Insert(disease,true);
					}
					catch(Oracle.DataAccess.Client.OracleException ex){
						if(ex.Number==1 && ex.Message.ToLower().Contains("unique constraint") && ex.Message.ToLower().Contains("violated")){
							disease.DiseaseNum++;
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
				return Insert(disease,false);
			}
		}

		///<summary>Inserts one Disease into the database.  Provides option to use the existing priKey.</summary>
		public static long Insert(Disease disease,bool useExistingPK){
			if(!useExistingPK && PrefC.RandomKeys) {
				disease.DiseaseNum=ReplicationServers.GetKey("disease","DiseaseNum");
			}
			string command="INSERT INTO disease (";
			if(useExistingPK || PrefC.RandomKeys) {
				command+="DiseaseNum,";
			}
			command+="PatNum,DiseaseDefNum,PatNote,ProbStatus,DateStart,DateStop,SnomedProblemType,FunctionStatus) VALUES(";
			if(useExistingPK || PrefC.RandomKeys) {
				command+=POut.Long(disease.DiseaseNum)+",";
			}
			command+=
				     POut.Long  (disease.PatNum)+","
				+    POut.Long  (disease.DiseaseDefNum)+","
				+"'"+POut.String(disease.PatNote)+"',"
				//DateTStamp can only be set by MySQL
				+    POut.Int   ((int)disease.ProbStatus)+","
				+    POut.Date  (disease.DateStart)+","
				+    POut.Date  (disease.DateStop)+","
				+"'"+POut.String(disease.SnomedProblemType)+"',"
				+    POut.Int   ((int)disease.FunctionStatus)+")";
			if(useExistingPK || PrefC.RandomKeys) {
				Db.NonQ(command);
			}
			else {
				disease.DiseaseNum=Db.NonQ(command,true);
			}
			return disease.DiseaseNum;
		}

		///<summary>Updates one Disease in the database.</summary>
		public static void Update(Disease disease){
			string command="UPDATE disease SET "
				+"PatNum           =  "+POut.Long  (disease.PatNum)+", "
				+"DiseaseDefNum    =  "+POut.Long  (disease.DiseaseDefNum)+", "
				+"PatNote          = '"+POut.String(disease.PatNote)+"', "
				//DateTStamp can only be set by MySQL
				+"ProbStatus       =  "+POut.Int   ((int)disease.ProbStatus)+", "
				+"DateStart        =  "+POut.Date  (disease.DateStart)+", "
				+"DateStop         =  "+POut.Date  (disease.DateStop)+", "
				+"SnomedProblemType= '"+POut.String(disease.SnomedProblemType)+"', "
				+"FunctionStatus   =  "+POut.Int   ((int)disease.FunctionStatus)+" "
				+"WHERE DiseaseNum = "+POut.Long(disease.DiseaseNum);
			Db.NonQ(command);
		}

		///<summary>Updates one Disease in the database.  Uses an old object to compare to, and only alters changed fields.  This prevents collisions and concurrency problems in heavily used tables.  Returns true if an update occurred.</summary>
		public static bool Update(Disease disease,Disease oldDisease){
			string command="";
			if(disease.PatNum != oldDisease.PatNum) {
				if(command!=""){ command+=",";}
				command+="PatNum = "+POut.Long(disease.PatNum)+"";
			}
			if(disease.DiseaseDefNum != oldDisease.DiseaseDefNum) {
				if(command!=""){ command+=",";}
				command+="DiseaseDefNum = "+POut.Long(disease.DiseaseDefNum)+"";
			}
			if(disease.PatNote != oldDisease.PatNote) {
				if(command!=""){ command+=",";}
				command+="PatNote = '"+POut.String(disease.PatNote)+"'";
			}
			//DateTStamp can only be set by MySQL
			if(disease.ProbStatus != oldDisease.ProbStatus) {
				if(command!=""){ command+=",";}
				command+="ProbStatus = "+POut.Int   ((int)disease.ProbStatus)+"";
			}
			if(disease.DateStart != oldDisease.DateStart) {
				if(command!=""){ command+=",";}
				command+="DateStart = "+POut.Date(disease.DateStart)+"";
			}
			if(disease.DateStop != oldDisease.DateStop) {
				if(command!=""){ command+=",";}
				command+="DateStop = "+POut.Date(disease.DateStop)+"";
			}
			if(disease.SnomedProblemType != oldDisease.SnomedProblemType) {
				if(command!=""){ command+=",";}
				command+="SnomedProblemType = '"+POut.String(disease.SnomedProblemType)+"'";
			}
			if(disease.FunctionStatus != oldDisease.FunctionStatus) {
				if(command!=""){ command+=",";}
				command+="FunctionStatus = "+POut.Int   ((int)disease.FunctionStatus)+"";
			}
			if(command==""){
				return false;
			}
			command="UPDATE disease SET "+command
				+" WHERE DiseaseNum = "+POut.Long(disease.DiseaseNum);
			Db.NonQ(command);
			return true;
		}

		///<summary>Deletes one Disease from the database.</summary>
		public static void Delete(long diseaseNum){
			string command="DELETE FROM disease "
				+"WHERE DiseaseNum = "+POut.Long(diseaseNum);
			Db.NonQ(command);
		}

	}
}