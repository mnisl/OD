//This file is automatically generated.
//Do not attempt to make changes to this file because the changes will be erased and overwritten.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;

namespace OpenDentBusiness.Crud{
	public class DiseaseDefCrud {
		///<summary>Gets one DiseaseDef object from the database using the primary key.  Returns null if not found.</summary>
		public static DiseaseDef SelectOne(long diseaseDefNum){
			string command="SELECT * FROM diseasedef "
				+"WHERE DiseaseDefNum = "+POut.Long(diseaseDefNum);
			List<DiseaseDef> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets one DiseaseDef object from the database using a query.</summary>
		public static DiseaseDef SelectOne(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<DiseaseDef> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets a list of DiseaseDef objects from the database using a query.</summary>
		public static List<DiseaseDef> SelectMany(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<DiseaseDef> list=TableToList(Db.GetTable(command));
			return list;
		}

		///<summary>Converts a DataTable to a list of objects.</summary>
		public static List<DiseaseDef> TableToList(DataTable table){
			List<DiseaseDef> retVal=new List<DiseaseDef>();
			DiseaseDef diseaseDef;
			for(int i=0;i<table.Rows.Count;i++) {
				diseaseDef=new DiseaseDef();
				diseaseDef.DiseaseDefNum= PIn.Long  (table.Rows[i]["DiseaseDefNum"].ToString());
				diseaseDef.DiseaseName  = PIn.String(table.Rows[i]["DiseaseName"].ToString());
				diseaseDef.ItemOrder    = PIn.Int   (table.Rows[i]["ItemOrder"].ToString());
				diseaseDef.IsHidden     = PIn.Bool  (table.Rows[i]["IsHidden"].ToString());
				diseaseDef.DateTStamp   = PIn.DateT (table.Rows[i]["DateTStamp"].ToString());
				diseaseDef.ICD9Code     = PIn.String(table.Rows[i]["ICD9Code"].ToString());
				diseaseDef.SnomedCode   = PIn.String(table.Rows[i]["SnomedCode"].ToString());
				diseaseDef.Icd10Code    = PIn.String(table.Rows[i]["Icd10Code"].ToString());
				retVal.Add(diseaseDef);
			}
			return retVal;
		}

		///<summary>Inserts one DiseaseDef into the database.  Returns the new priKey.</summary>
		public static long Insert(DiseaseDef diseaseDef){
			if(DataConnection.DBtype==DatabaseType.Oracle) {
				diseaseDef.DiseaseDefNum=DbHelper.GetNextOracleKey("diseasedef","DiseaseDefNum");
				int loopcount=0;
				while(loopcount<100){
					try {
						return Insert(diseaseDef,true);
					}
					catch(Oracle.DataAccess.Client.OracleException ex){
						if(ex.Number==1 && ex.Message.ToLower().Contains("unique constraint") && ex.Message.ToLower().Contains("violated")){
							diseaseDef.DiseaseDefNum++;
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
				return Insert(diseaseDef,false);
			}
		}

		///<summary>Inserts one DiseaseDef into the database.  Provides option to use the existing priKey.</summary>
		public static long Insert(DiseaseDef diseaseDef,bool useExistingPK){
			if(!useExistingPK && PrefC.RandomKeys) {
				diseaseDef.DiseaseDefNum=ReplicationServers.GetKey("diseasedef","DiseaseDefNum");
			}
			string command="INSERT INTO diseasedef (";
			if(useExistingPK || PrefC.RandomKeys) {
				command+="DiseaseDefNum,";
			}
			command+="DiseaseName,ItemOrder,IsHidden,ICD9Code,SnomedCode,Icd10Code) VALUES(";
			if(useExistingPK || PrefC.RandomKeys) {
				command+=POut.Long(diseaseDef.DiseaseDefNum)+",";
			}
			command+=
				 "'"+POut.String(diseaseDef.DiseaseName)+"',"
				+    POut.Int   (diseaseDef.ItemOrder)+","
				+    POut.Bool  (diseaseDef.IsHidden)+","
				//DateTStamp can only be set by MySQL
				+"'"+POut.String(diseaseDef.ICD9Code)+"',"
				+"'"+POut.String(diseaseDef.SnomedCode)+"',"
				+"'"+POut.String(diseaseDef.Icd10Code)+"')";
			if(useExistingPK || PrefC.RandomKeys) {
				Db.NonQ(command);
			}
			else {
				diseaseDef.DiseaseDefNum=Db.NonQ(command,true);
			}
			return diseaseDef.DiseaseDefNum;
		}

		///<summary>Updates one DiseaseDef in the database.</summary>
		public static void Update(DiseaseDef diseaseDef){
			string command="UPDATE diseasedef SET "
				+"DiseaseName  = '"+POut.String(diseaseDef.DiseaseName)+"', "
				+"ItemOrder    =  "+POut.Int   (diseaseDef.ItemOrder)+", "
				+"IsHidden     =  "+POut.Bool  (diseaseDef.IsHidden)+", "
				//DateTStamp can only be set by MySQL
				+"ICD9Code     = '"+POut.String(diseaseDef.ICD9Code)+"', "
				+"SnomedCode   = '"+POut.String(diseaseDef.SnomedCode)+"', "
				+"Icd10Code    = '"+POut.String(diseaseDef.Icd10Code)+"' "
				+"WHERE DiseaseDefNum = "+POut.Long(diseaseDef.DiseaseDefNum);
			Db.NonQ(command);
		}

		///<summary>Updates one DiseaseDef in the database.  Uses an old object to compare to, and only alters changed fields.  This prevents collisions and concurrency problems in heavily used tables.  Returns true if an update occurred.</summary>
		public static bool Update(DiseaseDef diseaseDef,DiseaseDef oldDiseaseDef){
			string command="";
			if(diseaseDef.DiseaseName != oldDiseaseDef.DiseaseName) {
				if(command!=""){ command+=",";}
				command+="DiseaseName = '"+POut.String(diseaseDef.DiseaseName)+"'";
			}
			if(diseaseDef.ItemOrder != oldDiseaseDef.ItemOrder) {
				if(command!=""){ command+=",";}
				command+="ItemOrder = "+POut.Int(diseaseDef.ItemOrder)+"";
			}
			if(diseaseDef.IsHidden != oldDiseaseDef.IsHidden) {
				if(command!=""){ command+=",";}
				command+="IsHidden = "+POut.Bool(diseaseDef.IsHidden)+"";
			}
			//DateTStamp can only be set by MySQL
			if(diseaseDef.ICD9Code != oldDiseaseDef.ICD9Code) {
				if(command!=""){ command+=",";}
				command+="ICD9Code = '"+POut.String(diseaseDef.ICD9Code)+"'";
			}
			if(diseaseDef.SnomedCode != oldDiseaseDef.SnomedCode) {
				if(command!=""){ command+=",";}
				command+="SnomedCode = '"+POut.String(diseaseDef.SnomedCode)+"'";
			}
			if(diseaseDef.Icd10Code != oldDiseaseDef.Icd10Code) {
				if(command!=""){ command+=",";}
				command+="Icd10Code = '"+POut.String(diseaseDef.Icd10Code)+"'";
			}
			if(command==""){
				return false;
			}
			command="UPDATE diseasedef SET "+command
				+" WHERE DiseaseDefNum = "+POut.Long(diseaseDef.DiseaseDefNum);
			Db.NonQ(command);
			return true;
		}

		///<summary>Deletes one DiseaseDef from the database.</summary>
		public static void Delete(long diseaseDefNum){
			string command="DELETE FROM diseasedef "
				+"WHERE DiseaseDefNum = "+POut.Long(diseaseDefNum);
			Db.NonQ(command);
		}

		///<summary>Inserts, updates, or deletes database rows to match supplied list.</summary>
		public static void Sync(List<DiseaseDef> listNew,List<DiseaseDef> listDB) {
			//Adding items to lists changes the order of operation. All inserts are completed first, then updates, then deletes.
			List<DiseaseDef> listIns    =new List<DiseaseDef>();
			List<DiseaseDef> listUpdNew =new List<DiseaseDef>();
			List<DiseaseDef> listUpdDB  =new List<DiseaseDef>();
			List<DiseaseDef> listDel    =new List<DiseaseDef>();
			listNew.Sort((DiseaseDef x,DiseaseDef y) => { return x.DiseaseDefNum.CompareTo(y.DiseaseDefNum); });//Anonymous function, sorts by compairing PK.  Lambda expressions are not allowed, this is the one and only exception.  JS approved.
			listDB.Sort((DiseaseDef x,DiseaseDef y) => { return x.DiseaseDefNum.CompareTo(y.DiseaseDefNum); });//Anonymous function, sorts by compairing PK.  Lambda expressions are not allowed, this is the one and only exception.  JS approved.
			int idxNew=0;
			int idxDB=0;
			DiseaseDef fieldNew;
			DiseaseDef fieldDB;
			//Because both lists have been sorted using the same criteria, we can now walk each list to determine which list contians the next element.  The next element is determined by Primary Key.
			//If the New list contains the next item it will be inserted.  If the DB contains the next item, it will be deleted.  If both lists contain the next item, the item will be updated.
			while(idxNew<listNew.Count || idxDB<listDB.Count) {
				fieldNew=null;
				if(idxNew<listNew.Count) {
					fieldNew=listNew[idxNew];
				}
				fieldDB=null;
				if(idxDB<listDB.Count) {
					fieldDB=listDB[idxDB];
				}
				//begin compare
				if(fieldNew!=null && fieldDB==null) {//listNew has more items, listDB does not.
					listIns.Add(fieldNew);
					idxNew++;
					continue;
				}
				else if(fieldNew==null && fieldDB!=null) {//listDB has more items, listNew does not.
					listDel.Add(fieldDB);
					idxDB++;
					continue;
				}
				else if(fieldNew.DiseaseDefNum<fieldDB.DiseaseDefNum) {//newPK less than dbPK, newItem is 'next'
					listIns.Add(fieldNew);
					idxNew++;
					continue;
				}
				else if(fieldNew.DiseaseDefNum>fieldDB.DiseaseDefNum) {//dbPK less than newPK, dbItem is 'next'
					listDel.Add(fieldDB);
					idxDB++;
					continue;
				}
				//Both lists contain the 'next' item, update required
				listUpdNew.Add(fieldNew);
				listUpdDB.Add(fieldDB);
				idxNew++;
				idxDB++;
			}
			//Commit changes to DB
			for(int i=0;i<listIns.Count;i++) {
				Insert(listIns[i]);
			}
			for(int i=0;i<listUpdNew.Count;i++) {
				Update(listUpdNew[i],listUpdDB[i]);
			}
			for(int i=0;i<listDel.Count;i++) {
				Delete(listDel[i].DiseaseDefNum);
			}
		}

	}
}