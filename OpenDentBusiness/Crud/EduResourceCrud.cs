//This file is automatically generated.
//Do not attempt to make changes to this file because the changes will be erased and overwritten.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;

namespace OpenDentBusiness.Crud{
	public class EduResourceCrud {
		///<summary>Gets one EduResource object from the database using the primary key.  Returns null if not found.</summary>
		public static EduResource SelectOne(long eduResourceNum){
			string command="SELECT * FROM eduresource "
				+"WHERE EduResourceNum = "+POut.Long(eduResourceNum);
			List<EduResource> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets one EduResource object from the database using a query.</summary>
		public static EduResource SelectOne(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<EduResource> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets a list of EduResource objects from the database using a query.</summary>
		public static List<EduResource> SelectMany(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<EduResource> list=TableToList(Db.GetTable(command));
			return list;
		}

		///<summary>Converts a DataTable to a list of objects.</summary>
		public static List<EduResource> TableToList(DataTable table){
			List<EduResource> retVal=new List<EduResource>();
			EduResource eduResource;
			for(int i=0;i<table.Rows.Count;i++) {
				eduResource=new EduResource();
				eduResource.EduResourceNum  = PIn.Long  (table.Rows[i]["EduResourceNum"].ToString());
				eduResource.DiseaseDefNum   = PIn.Long  (table.Rows[i]["DiseaseDefNum"].ToString());
				eduResource.MedicationNum   = PIn.Long  (table.Rows[i]["MedicationNum"].ToString());
				eduResource.LabResultID     = PIn.String(table.Rows[i]["LabResultID"].ToString());
				eduResource.LabResultName   = PIn.String(table.Rows[i]["LabResultName"].ToString());
				eduResource.LabResultCompare= PIn.String(table.Rows[i]["LabResultCompare"].ToString());
				eduResource.ResourceUrl     = PIn.String(table.Rows[i]["ResourceUrl"].ToString());
				retVal.Add(eduResource);
			}
			return retVal;
		}

		///<summary>Inserts one EduResource into the database.  Returns the new priKey.</summary>
		public static long Insert(EduResource eduResource){
			if(DataConnection.DBtype==DatabaseType.Oracle) {
				eduResource.EduResourceNum=DbHelper.GetNextOracleKey("eduresource","EduResourceNum");
				int loopcount=0;
				while(loopcount<100){
					try {
						return Insert(eduResource,true);
					}
					catch(Oracle.DataAccess.Client.OracleException ex){
						if(ex.Number==1 && ex.Message.ToLower().Contains("unique constraint") && ex.Message.ToLower().Contains("violated")){
							eduResource.EduResourceNum++;
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
				return Insert(eduResource,false);
			}
		}

		///<summary>Inserts one EduResource into the database.  Provides option to use the existing priKey.</summary>
		public static long Insert(EduResource eduResource,bool useExistingPK){
			if(!useExistingPK && PrefC.RandomKeys) {
				eduResource.EduResourceNum=ReplicationServers.GetKey("eduresource","EduResourceNum");
			}
			string command="INSERT INTO eduresource (";
			if(useExistingPK || PrefC.RandomKeys) {
				command+="EduResourceNum,";
			}
			command+="DiseaseDefNum,MedicationNum,LabResultID,LabResultName,LabResultCompare,ResourceUrl) VALUES(";
			if(useExistingPK || PrefC.RandomKeys) {
				command+=POut.Long(eduResource.EduResourceNum)+",";
			}
			command+=
				     POut.Long  (eduResource.DiseaseDefNum)+","
				+    POut.Long  (eduResource.MedicationNum)+","
				+"'"+POut.String(eduResource.LabResultID)+"',"
				+"'"+POut.String(eduResource.LabResultName)+"',"
				+"'"+POut.String(eduResource.LabResultCompare)+"',"
				+"'"+POut.String(eduResource.ResourceUrl)+"')";
			if(useExistingPK || PrefC.RandomKeys) {
				Db.NonQ(command);
			}
			else {
				eduResource.EduResourceNum=Db.NonQ(command,true);
			}
			return eduResource.EduResourceNum;
		}

		///<summary>Updates one EduResource in the database.</summary>
		public static void Update(EduResource eduResource){
			string command="UPDATE eduresource SET "
				+"DiseaseDefNum   =  "+POut.Long  (eduResource.DiseaseDefNum)+", "
				+"MedicationNum   =  "+POut.Long  (eduResource.MedicationNum)+", "
				+"LabResultID     = '"+POut.String(eduResource.LabResultID)+"', "
				+"LabResultName   = '"+POut.String(eduResource.LabResultName)+"', "
				+"LabResultCompare= '"+POut.String(eduResource.LabResultCompare)+"', "
				+"ResourceUrl     = '"+POut.String(eduResource.ResourceUrl)+"' "
				+"WHERE EduResourceNum = "+POut.Long(eduResource.EduResourceNum);
			Db.NonQ(command);
		}

		///<summary>Updates one EduResource in the database.  Uses an old object to compare to, and only alters changed fields.  This prevents collisions and concurrency problems in heavily used tables.  Returns true if an update occurred.</summary>
		public static bool Update(EduResource eduResource,EduResource oldEduResource){
			string command="";
			if(eduResource.DiseaseDefNum != oldEduResource.DiseaseDefNum) {
				if(command!=""){ command+=",";}
				command+="DiseaseDefNum = "+POut.Long(eduResource.DiseaseDefNum)+"";
			}
			if(eduResource.MedicationNum != oldEduResource.MedicationNum) {
				if(command!=""){ command+=",";}
				command+="MedicationNum = "+POut.Long(eduResource.MedicationNum)+"";
			}
			if(eduResource.LabResultID != oldEduResource.LabResultID) {
				if(command!=""){ command+=",";}
				command+="LabResultID = '"+POut.String(eduResource.LabResultID)+"'";
			}
			if(eduResource.LabResultName != oldEduResource.LabResultName) {
				if(command!=""){ command+=",";}
				command+="LabResultName = '"+POut.String(eduResource.LabResultName)+"'";
			}
			if(eduResource.LabResultCompare != oldEduResource.LabResultCompare) {
				if(command!=""){ command+=",";}
				command+="LabResultCompare = '"+POut.String(eduResource.LabResultCompare)+"'";
			}
			if(eduResource.ResourceUrl != oldEduResource.ResourceUrl) {
				if(command!=""){ command+=",";}
				command+="ResourceUrl = '"+POut.String(eduResource.ResourceUrl)+"'";
			}
			if(command==""){
				return false;
			}
			command="UPDATE eduresource SET "+command
				+" WHERE EduResourceNum = "+POut.Long(eduResource.EduResourceNum);
			Db.NonQ(command);
			return true;
		}

		///<summary>Deletes one EduResource from the database.</summary>
		public static void Delete(long eduResourceNum){
			string command="DELETE FROM eduresource "
				+"WHERE EduResourceNum = "+POut.Long(eduResourceNum);
			Db.NonQ(command);
		}

	}
}