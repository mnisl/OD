//This file is automatically generated.
//Do not attempt to make changes to this file because the changes will be erased and overwritten.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;

namespace OpenDentBusiness.Crud{
	public class MedLabFacAttachCrud {
		///<summary>Gets one MedLabFacAttach object from the database using the primary key.  Returns null if not found.</summary>
		public static MedLabFacAttach SelectOne(long medLabFacAttachNum){
			string command="SELECT * FROM medlabfacattach "
				+"WHERE MedLabFacAttachNum = "+POut.Long(medLabFacAttachNum);
			List<MedLabFacAttach> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets one MedLabFacAttach object from the database using a query.</summary>
		public static MedLabFacAttach SelectOne(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<MedLabFacAttach> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets a list of MedLabFacAttach objects from the database using a query.</summary>
		public static List<MedLabFacAttach> SelectMany(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<MedLabFacAttach> list=TableToList(Db.GetTable(command));
			return list;
		}

		///<summary>Converts a DataTable to a list of objects.</summary>
		public static List<MedLabFacAttach> TableToList(DataTable table){
			List<MedLabFacAttach> retVal=new List<MedLabFacAttach>();
			MedLabFacAttach medLabFacAttach;
			for(int i=0;i<table.Rows.Count;i++) {
				medLabFacAttach=new MedLabFacAttach();
				medLabFacAttach.MedLabFacAttachNum= PIn.Long  (table.Rows[i]["MedLabFacAttachNum"].ToString());
				medLabFacAttach.MedLabNum         = PIn.Long  (table.Rows[i]["MedLabNum"].ToString());
				medLabFacAttach.MedLabResultNum   = PIn.Long  (table.Rows[i]["MedLabResultNum"].ToString());
				medLabFacAttach.MedLabFacilityNum = PIn.Long  (table.Rows[i]["MedLabFacilityNum"].ToString());
				retVal.Add(medLabFacAttach);
			}
			return retVal;
		}

		///<summary>Inserts one MedLabFacAttach into the database.  Returns the new priKey.</summary>
		public static long Insert(MedLabFacAttach medLabFacAttach){
			if(DataConnection.DBtype==DatabaseType.Oracle) {
				medLabFacAttach.MedLabFacAttachNum=DbHelper.GetNextOracleKey("medlabfacattach","MedLabFacAttachNum");
				int loopcount=0;
				while(loopcount<100){
					try {
						return Insert(medLabFacAttach,true);
					}
					catch(Oracle.DataAccess.Client.OracleException ex){
						if(ex.Number==1 && ex.Message.ToLower().Contains("unique constraint") && ex.Message.ToLower().Contains("violated")){
							medLabFacAttach.MedLabFacAttachNum++;
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
				return Insert(medLabFacAttach,false);
			}
		}

		///<summary>Inserts one MedLabFacAttach into the database.  Provides option to use the existing priKey.</summary>
		public static long Insert(MedLabFacAttach medLabFacAttach,bool useExistingPK){
			if(!useExistingPK && PrefC.RandomKeys) {
				medLabFacAttach.MedLabFacAttachNum=ReplicationServers.GetKey("medlabfacattach","MedLabFacAttachNum");
			}
			string command="INSERT INTO medlabfacattach (";
			if(useExistingPK || PrefC.RandomKeys) {
				command+="MedLabFacAttachNum,";
			}
			command+="MedLabNum,MedLabResultNum,MedLabFacilityNum) VALUES(";
			if(useExistingPK || PrefC.RandomKeys) {
				command+=POut.Long(medLabFacAttach.MedLabFacAttachNum)+",";
			}
			command+=
				     POut.Long  (medLabFacAttach.MedLabNum)+","
				+    POut.Long  (medLabFacAttach.MedLabResultNum)+","
				+    POut.Long  (medLabFacAttach.MedLabFacilityNum)+")";
			if(useExistingPK || PrefC.RandomKeys) {
				Db.NonQ(command);
			}
			else {
				medLabFacAttach.MedLabFacAttachNum=Db.NonQ(command,true);
			}
			return medLabFacAttach.MedLabFacAttachNum;
		}

		///<summary>Updates one MedLabFacAttach in the database.</summary>
		public static void Update(MedLabFacAttach medLabFacAttach){
			string command="UPDATE medlabfacattach SET "
				+"MedLabNum         =  "+POut.Long  (medLabFacAttach.MedLabNum)+", "
				+"MedLabResultNum   =  "+POut.Long  (medLabFacAttach.MedLabResultNum)+", "
				+"MedLabFacilityNum =  "+POut.Long  (medLabFacAttach.MedLabFacilityNum)+" "
				+"WHERE MedLabFacAttachNum = "+POut.Long(medLabFacAttach.MedLabFacAttachNum);
			Db.NonQ(command);
		}

		///<summary>Updates one MedLabFacAttach in the database.  Uses an old object to compare to, and only alters changed fields.  This prevents collisions and concurrency problems in heavily used tables.  Returns true if an update occurred.</summary>
		public static bool Update(MedLabFacAttach medLabFacAttach,MedLabFacAttach oldMedLabFacAttach){
			string command="";
			if(medLabFacAttach.MedLabNum != oldMedLabFacAttach.MedLabNum) {
				if(command!=""){ command+=",";}
				command+="MedLabNum = "+POut.Long(medLabFacAttach.MedLabNum)+"";
			}
			if(medLabFacAttach.MedLabResultNum != oldMedLabFacAttach.MedLabResultNum) {
				if(command!=""){ command+=",";}
				command+="MedLabResultNum = "+POut.Long(medLabFacAttach.MedLabResultNum)+"";
			}
			if(medLabFacAttach.MedLabFacilityNum != oldMedLabFacAttach.MedLabFacilityNum) {
				if(command!=""){ command+=",";}
				command+="MedLabFacilityNum = "+POut.Long(medLabFacAttach.MedLabFacilityNum)+"";
			}
			if(command==""){
				return false;
			}
			command="UPDATE medlabfacattach SET "+command
				+" WHERE MedLabFacAttachNum = "+POut.Long(medLabFacAttach.MedLabFacAttachNum);
			Db.NonQ(command);
			return true;
		}

		///<summary>Deletes one MedLabFacAttach from the database.</summary>
		public static void Delete(long medLabFacAttachNum){
			string command="DELETE FROM medlabfacattach "
				+"WHERE MedLabFacAttachNum = "+POut.Long(medLabFacAttachNum);
			Db.NonQ(command);
		}

	}
}