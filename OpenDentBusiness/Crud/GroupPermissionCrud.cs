//This file is automatically generated.
//Do not attempt to make changes to this file because the changes will be erased and overwritten.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;

namespace OpenDentBusiness.Crud{
	public class GroupPermissionCrud {
		///<summary>Gets one GroupPermission object from the database using the primary key.  Returns null if not found.</summary>
		public static GroupPermission SelectOne(long groupPermNum){
			string command="SELECT * FROM grouppermission "
				+"WHERE GroupPermNum = "+POut.Long(groupPermNum);
			List<GroupPermission> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets one GroupPermission object from the database using a query.</summary>
		public static GroupPermission SelectOne(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<GroupPermission> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets a list of GroupPermission objects from the database using a query.</summary>
		public static List<GroupPermission> SelectMany(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<GroupPermission> list=TableToList(Db.GetTable(command));
			return list;
		}

		///<summary>Converts a DataTable to a list of objects.</summary>
		public static List<GroupPermission> TableToList(DataTable table){
			List<GroupPermission> retVal=new List<GroupPermission>();
			GroupPermission groupPermission;
			for(int i=0;i<table.Rows.Count;i++) {
				groupPermission=new GroupPermission();
				groupPermission.GroupPermNum= PIn.Long  (table.Rows[i]["GroupPermNum"].ToString());
				groupPermission.NewerDate   = PIn.Date  (table.Rows[i]["NewerDate"].ToString());
				groupPermission.NewerDays   = PIn.Int   (table.Rows[i]["NewerDays"].ToString());
				groupPermission.UserGroupNum= PIn.Long  (table.Rows[i]["UserGroupNum"].ToString());
				groupPermission.PermType    = (OpenDentBusiness.Permissions)PIn.Int(table.Rows[i]["PermType"].ToString());
				retVal.Add(groupPermission);
			}
			return retVal;
		}

		///<summary>Inserts one GroupPermission into the database.  Returns the new priKey.</summary>
		public static long Insert(GroupPermission groupPermission){
			if(DataConnection.DBtype==DatabaseType.Oracle) {
				groupPermission.GroupPermNum=DbHelper.GetNextOracleKey("grouppermission","GroupPermNum");
				int loopcount=0;
				while(loopcount<100){
					try {
						return Insert(groupPermission,true);
					}
					catch(Oracle.DataAccess.Client.OracleException ex){
						if(ex.Number==1 && ex.Message.ToLower().Contains("unique constraint") && ex.Message.ToLower().Contains("violated")){
							groupPermission.GroupPermNum++;
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
				return Insert(groupPermission,false);
			}
		}

		///<summary>Inserts one GroupPermission into the database.  Provides option to use the existing priKey.</summary>
		public static long Insert(GroupPermission groupPermission,bool useExistingPK){
			if(!useExistingPK && PrefC.RandomKeys) {
				groupPermission.GroupPermNum=ReplicationServers.GetKey("grouppermission","GroupPermNum");
			}
			string command="INSERT INTO grouppermission (";
			if(useExistingPK || PrefC.RandomKeys) {
				command+="GroupPermNum,";
			}
			command+="NewerDate,NewerDays,UserGroupNum,PermType) VALUES(";
			if(useExistingPK || PrefC.RandomKeys) {
				command+=POut.Long(groupPermission.GroupPermNum)+",";
			}
			command+=
				     POut.Date  (groupPermission.NewerDate)+","
				+    POut.Int   (groupPermission.NewerDays)+","
				+    POut.Long  (groupPermission.UserGroupNum)+","
				+    POut.Int   ((int)groupPermission.PermType)+")";
			if(useExistingPK || PrefC.RandomKeys) {
				Db.NonQ(command);
			}
			else {
				groupPermission.GroupPermNum=Db.NonQ(command,true);
			}
			return groupPermission.GroupPermNum;
		}

		///<summary>Updates one GroupPermission in the database.</summary>
		public static void Update(GroupPermission groupPermission){
			string command="UPDATE grouppermission SET "
				+"NewerDate   =  "+POut.Date  (groupPermission.NewerDate)+", "
				+"NewerDays   =  "+POut.Int   (groupPermission.NewerDays)+", "
				+"UserGroupNum=  "+POut.Long  (groupPermission.UserGroupNum)+", "
				+"PermType    =  "+POut.Int   ((int)groupPermission.PermType)+" "
				+"WHERE GroupPermNum = "+POut.Long(groupPermission.GroupPermNum);
			Db.NonQ(command);
		}

		///<summary>Updates one GroupPermission in the database.  Uses an old object to compare to, and only alters changed fields.  This prevents collisions and concurrency problems in heavily used tables.  Returns true if an update occurred.</summary>
		public static bool Update(GroupPermission groupPermission,GroupPermission oldGroupPermission){
			string command="";
			if(groupPermission.NewerDate != oldGroupPermission.NewerDate) {
				if(command!=""){ command+=",";}
				command+="NewerDate = "+POut.Date(groupPermission.NewerDate)+"";
			}
			if(groupPermission.NewerDays != oldGroupPermission.NewerDays) {
				if(command!=""){ command+=",";}
				command+="NewerDays = "+POut.Int(groupPermission.NewerDays)+"";
			}
			if(groupPermission.UserGroupNum != oldGroupPermission.UserGroupNum) {
				if(command!=""){ command+=",";}
				command+="UserGroupNum = "+POut.Long(groupPermission.UserGroupNum)+"";
			}
			if(groupPermission.PermType != oldGroupPermission.PermType) {
				if(command!=""){ command+=",";}
				command+="PermType = "+POut.Int   ((int)groupPermission.PermType)+"";
			}
			if(command==""){
				return false;
			}
			command="UPDATE grouppermission SET "+command
				+" WHERE GroupPermNum = "+POut.Long(groupPermission.GroupPermNum);
			Db.NonQ(command);
			return true;
		}

		///<summary>Deletes one GroupPermission from the database.</summary>
		public static void Delete(long groupPermNum){
			string command="DELETE FROM grouppermission "
				+"WHERE GroupPermNum = "+POut.Long(groupPermNum);
			Db.NonQ(command);
		}

	}
}