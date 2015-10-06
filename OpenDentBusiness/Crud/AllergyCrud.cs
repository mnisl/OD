//This file is automatically generated.
//Do not attempt to make changes to this file because the changes will be erased and overwritten.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;

namespace OpenDentBusiness.Crud{
	public class AllergyCrud {
		///<summary>Gets one Allergy object from the database using the primary key.  Returns null if not found.</summary>
		public static Allergy SelectOne(long allergyNum){
			string command="SELECT * FROM allergy "
				+"WHERE AllergyNum = "+POut.Long(allergyNum);
			List<Allergy> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets one Allergy object from the database using a query.</summary>
		public static Allergy SelectOne(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<Allergy> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets a list of Allergy objects from the database using a query.</summary>
		public static List<Allergy> SelectMany(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<Allergy> list=TableToList(Db.GetTable(command));
			return list;
		}

		///<summary>Converts a DataTable to a list of objects.</summary>
		public static List<Allergy> TableToList(DataTable table){
			List<Allergy> retVal=new List<Allergy>();
			Allergy allergy;
			for(int i=0;i<table.Rows.Count;i++) {
				allergy=new Allergy();
				allergy.AllergyNum         = PIn.Long  (table.Rows[i]["AllergyNum"].ToString());
				allergy.AllergyDefNum      = PIn.Long  (table.Rows[i]["AllergyDefNum"].ToString());
				allergy.PatNum             = PIn.Long  (table.Rows[i]["PatNum"].ToString());
				allergy.Reaction           = PIn.String(table.Rows[i]["Reaction"].ToString());
				allergy.StatusIsActive     = PIn.Bool  (table.Rows[i]["StatusIsActive"].ToString());
				allergy.DateTStamp         = PIn.DateT (table.Rows[i]["DateTStamp"].ToString());
				allergy.DateAdverseReaction= PIn.Date  (table.Rows[i]["DateAdverseReaction"].ToString());
				allergy.SnomedReaction     = PIn.String(table.Rows[i]["SnomedReaction"].ToString());
				retVal.Add(allergy);
			}
			return retVal;
		}

		///<summary>Inserts one Allergy into the database.  Returns the new priKey.</summary>
		public static long Insert(Allergy allergy){
			if(DataConnection.DBtype==DatabaseType.Oracle) {
				allergy.AllergyNum=DbHelper.GetNextOracleKey("allergy","AllergyNum");
				int loopcount=0;
				while(loopcount<100){
					try {
						return Insert(allergy,true);
					}
					catch(Oracle.DataAccess.Client.OracleException ex){
						if(ex.Number==1 && ex.Message.ToLower().Contains("unique constraint") && ex.Message.ToLower().Contains("violated")){
							allergy.AllergyNum++;
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
				return Insert(allergy,false);
			}
		}

		///<summary>Inserts one Allergy into the database.  Provides option to use the existing priKey.</summary>
		public static long Insert(Allergy allergy,bool useExistingPK){
			if(!useExistingPK && PrefC.RandomKeys) {
				allergy.AllergyNum=ReplicationServers.GetKey("allergy","AllergyNum");
			}
			string command="INSERT INTO allergy (";
			if(useExistingPK || PrefC.RandomKeys) {
				command+="AllergyNum,";
			}
			command+="AllergyDefNum,PatNum,Reaction,StatusIsActive,DateAdverseReaction,SnomedReaction) VALUES(";
			if(useExistingPK || PrefC.RandomKeys) {
				command+=POut.Long(allergy.AllergyNum)+",";
			}
			command+=
				     POut.Long  (allergy.AllergyDefNum)+","
				+    POut.Long  (allergy.PatNum)+","
				+"'"+POut.String(allergy.Reaction)+"',"
				+    POut.Bool  (allergy.StatusIsActive)+","
				//DateTStamp can only be set by MySQL
				+    POut.Date  (allergy.DateAdverseReaction)+","
				+"'"+POut.String(allergy.SnomedReaction)+"')";
			if(useExistingPK || PrefC.RandomKeys) {
				Db.NonQ(command);
			}
			else {
				allergy.AllergyNum=Db.NonQ(command,true);
			}
			return allergy.AllergyNum;
		}

		///<summary>Updates one Allergy in the database.</summary>
		public static void Update(Allergy allergy){
			string command="UPDATE allergy SET "
				+"AllergyDefNum      =  "+POut.Long  (allergy.AllergyDefNum)+", "
				+"PatNum             =  "+POut.Long  (allergy.PatNum)+", "
				+"Reaction           = '"+POut.String(allergy.Reaction)+"', "
				+"StatusIsActive     =  "+POut.Bool  (allergy.StatusIsActive)+", "
				//DateTStamp can only be set by MySQL
				+"DateAdverseReaction=  "+POut.Date  (allergy.DateAdverseReaction)+", "
				+"SnomedReaction     = '"+POut.String(allergy.SnomedReaction)+"' "
				+"WHERE AllergyNum = "+POut.Long(allergy.AllergyNum);
			Db.NonQ(command);
		}

		///<summary>Updates one Allergy in the database.  Uses an old object to compare to, and only alters changed fields.  This prevents collisions and concurrency problems in heavily used tables.  Returns true if an update occurred.</summary>
		public static bool Update(Allergy allergy,Allergy oldAllergy){
			string command="";
			if(allergy.AllergyDefNum != oldAllergy.AllergyDefNum) {
				if(command!=""){ command+=",";}
				command+="AllergyDefNum = "+POut.Long(allergy.AllergyDefNum)+"";
			}
			if(allergy.PatNum != oldAllergy.PatNum) {
				if(command!=""){ command+=",";}
				command+="PatNum = "+POut.Long(allergy.PatNum)+"";
			}
			if(allergy.Reaction != oldAllergy.Reaction) {
				if(command!=""){ command+=",";}
				command+="Reaction = '"+POut.String(allergy.Reaction)+"'";
			}
			if(allergy.StatusIsActive != oldAllergy.StatusIsActive) {
				if(command!=""){ command+=",";}
				command+="StatusIsActive = "+POut.Bool(allergy.StatusIsActive)+"";
			}
			//DateTStamp can only be set by MySQL
			if(allergy.DateAdverseReaction != oldAllergy.DateAdverseReaction) {
				if(command!=""){ command+=",";}
				command+="DateAdverseReaction = "+POut.Date(allergy.DateAdverseReaction)+"";
			}
			if(allergy.SnomedReaction != oldAllergy.SnomedReaction) {
				if(command!=""){ command+=",";}
				command+="SnomedReaction = '"+POut.String(allergy.SnomedReaction)+"'";
			}
			if(command==""){
				return false;
			}
			command="UPDATE allergy SET "+command
				+" WHERE AllergyNum = "+POut.Long(allergy.AllergyNum);
			Db.NonQ(command);
			return true;
		}

		///<summary>Deletes one Allergy from the database.</summary>
		public static void Delete(long allergyNum){
			string command="DELETE FROM allergy "
				+"WHERE AllergyNum = "+POut.Long(allergyNum);
			Db.NonQ(command);
		}

	}
}