//This file is automatically generated.
//Do not attempt to make changes to this file because the changes will be erased and overwritten.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;

namespace OpenDentBusiness.Crud{
	public class EvaluationCriterionCrud {
		///<summary>Gets one EvaluationCriterion object from the database using the primary key.  Returns null if not found.</summary>
		public static EvaluationCriterion SelectOne(long evaluationCriterionNum){
			string command="SELECT * FROM evaluationcriterion "
				+"WHERE EvaluationCriterionNum = "+POut.Long(evaluationCriterionNum);
			List<EvaluationCriterion> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets one EvaluationCriterion object from the database using a query.</summary>
		public static EvaluationCriterion SelectOne(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<EvaluationCriterion> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets a list of EvaluationCriterion objects from the database using a query.</summary>
		public static List<EvaluationCriterion> SelectMany(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<EvaluationCriterion> list=TableToList(Db.GetTable(command));
			return list;
		}

		///<summary>Converts a DataTable to a list of objects.</summary>
		public static List<EvaluationCriterion> TableToList(DataTable table){
			List<EvaluationCriterion> retVal=new List<EvaluationCriterion>();
			EvaluationCriterion evaluationCriterion;
			for(int i=0;i<table.Rows.Count;i++) {
				evaluationCriterion=new EvaluationCriterion();
				evaluationCriterion.EvaluationCriterionNum= PIn.Long  (table.Rows[i]["EvaluationCriterionNum"].ToString());
				evaluationCriterion.EvaluationNum         = PIn.Long  (table.Rows[i]["EvaluationNum"].ToString());
				evaluationCriterion.CriterionDescript     = PIn.String(table.Rows[i]["CriterionDescript"].ToString());
				evaluationCriterion.IsCategoryName        = PIn.Bool  (table.Rows[i]["IsCategoryName"].ToString());
				evaluationCriterion.GradingScaleNum       = PIn.Long  (table.Rows[i]["GradingScaleNum"].ToString());
				evaluationCriterion.GradeShowing          = PIn.String(table.Rows[i]["GradeShowing"].ToString());
				evaluationCriterion.GradeNumber           = PIn.Float (table.Rows[i]["GradeNumber"].ToString());
				evaluationCriterion.Notes                 = PIn.String(table.Rows[i]["Notes"].ToString());
				evaluationCriterion.ItemOrder             = PIn.Int   (table.Rows[i]["ItemOrder"].ToString());
				evaluationCriterion.MaxPointsPoss         = PIn.Float (table.Rows[i]["MaxPointsPoss"].ToString());
				retVal.Add(evaluationCriterion);
			}
			return retVal;
		}

		///<summary>Inserts one EvaluationCriterion into the database.  Returns the new priKey.</summary>
		public static long Insert(EvaluationCriterion evaluationCriterion){
			if(DataConnection.DBtype==DatabaseType.Oracle) {
				evaluationCriterion.EvaluationCriterionNum=DbHelper.GetNextOracleKey("evaluationcriterion","EvaluationCriterionNum");
				int loopcount=0;
				while(loopcount<100){
					try {
						return Insert(evaluationCriterion,true);
					}
					catch(Oracle.DataAccess.Client.OracleException ex){
						if(ex.Number==1 && ex.Message.ToLower().Contains("unique constraint") && ex.Message.ToLower().Contains("violated")){
							evaluationCriterion.EvaluationCriterionNum++;
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
				return Insert(evaluationCriterion,false);
			}
		}

		///<summary>Inserts one EvaluationCriterion into the database.  Provides option to use the existing priKey.</summary>
		public static long Insert(EvaluationCriterion evaluationCriterion,bool useExistingPK){
			if(!useExistingPK && PrefC.RandomKeys) {
				evaluationCriterion.EvaluationCriterionNum=ReplicationServers.GetKey("evaluationcriterion","EvaluationCriterionNum");
			}
			string command="INSERT INTO evaluationcriterion (";
			if(useExistingPK || PrefC.RandomKeys) {
				command+="EvaluationCriterionNum,";
			}
			command+="EvaluationNum,CriterionDescript,IsCategoryName,GradingScaleNum,GradeShowing,GradeNumber,Notes,ItemOrder,MaxPointsPoss) VALUES(";
			if(useExistingPK || PrefC.RandomKeys) {
				command+=POut.Long(evaluationCriterion.EvaluationCriterionNum)+",";
			}
			command+=
				     POut.Long  (evaluationCriterion.EvaluationNum)+","
				+"'"+POut.String(evaluationCriterion.CriterionDescript)+"',"
				+    POut.Bool  (evaluationCriterion.IsCategoryName)+","
				+    POut.Long  (evaluationCriterion.GradingScaleNum)+","
				+"'"+POut.String(evaluationCriterion.GradeShowing)+"',"
				+    POut.Float (evaluationCriterion.GradeNumber)+","
				+"'"+POut.String(evaluationCriterion.Notes)+"',"
				+    POut.Int   (evaluationCriterion.ItemOrder)+","
				+    POut.Float (evaluationCriterion.MaxPointsPoss)+")";
			if(useExistingPK || PrefC.RandomKeys) {
				Db.NonQ(command);
			}
			else {
				evaluationCriterion.EvaluationCriterionNum=Db.NonQ(command,true);
			}
			return evaluationCriterion.EvaluationCriterionNum;
		}

		///<summary>Updates one EvaluationCriterion in the database.</summary>
		public static void Update(EvaluationCriterion evaluationCriterion){
			string command="UPDATE evaluationcriterion SET "
				+"EvaluationNum         =  "+POut.Long  (evaluationCriterion.EvaluationNum)+", "
				+"CriterionDescript     = '"+POut.String(evaluationCriterion.CriterionDescript)+"', "
				+"IsCategoryName        =  "+POut.Bool  (evaluationCriterion.IsCategoryName)+", "
				+"GradingScaleNum       =  "+POut.Long  (evaluationCriterion.GradingScaleNum)+", "
				+"GradeShowing          = '"+POut.String(evaluationCriterion.GradeShowing)+"', "
				+"GradeNumber           =  "+POut.Float (evaluationCriterion.GradeNumber)+", "
				+"Notes                 = '"+POut.String(evaluationCriterion.Notes)+"', "
				+"ItemOrder             =  "+POut.Int   (evaluationCriterion.ItemOrder)+", "
				+"MaxPointsPoss         =  "+POut.Float (evaluationCriterion.MaxPointsPoss)+" "
				+"WHERE EvaluationCriterionNum = "+POut.Long(evaluationCriterion.EvaluationCriterionNum);
			Db.NonQ(command);
		}

		///<summary>Updates one EvaluationCriterion in the database.  Uses an old object to compare to, and only alters changed fields.  This prevents collisions and concurrency problems in heavily used tables.  Returns true if an update occurred.</summary>
		public static bool Update(EvaluationCriterion evaluationCriterion,EvaluationCriterion oldEvaluationCriterion){
			string command="";
			if(evaluationCriterion.EvaluationNum != oldEvaluationCriterion.EvaluationNum) {
				if(command!=""){ command+=",";}
				command+="EvaluationNum = "+POut.Long(evaluationCriterion.EvaluationNum)+"";
			}
			if(evaluationCriterion.CriterionDescript != oldEvaluationCriterion.CriterionDescript) {
				if(command!=""){ command+=",";}
				command+="CriterionDescript = '"+POut.String(evaluationCriterion.CriterionDescript)+"'";
			}
			if(evaluationCriterion.IsCategoryName != oldEvaluationCriterion.IsCategoryName) {
				if(command!=""){ command+=",";}
				command+="IsCategoryName = "+POut.Bool(evaluationCriterion.IsCategoryName)+"";
			}
			if(evaluationCriterion.GradingScaleNum != oldEvaluationCriterion.GradingScaleNum) {
				if(command!=""){ command+=",";}
				command+="GradingScaleNum = "+POut.Long(evaluationCriterion.GradingScaleNum)+"";
			}
			if(evaluationCriterion.GradeShowing != oldEvaluationCriterion.GradeShowing) {
				if(command!=""){ command+=",";}
				command+="GradeShowing = '"+POut.String(evaluationCriterion.GradeShowing)+"'";
			}
			if(evaluationCriterion.GradeNumber != oldEvaluationCriterion.GradeNumber) {
				if(command!=""){ command+=",";}
				command+="GradeNumber = "+POut.Float(evaluationCriterion.GradeNumber)+"";
			}
			if(evaluationCriterion.Notes != oldEvaluationCriterion.Notes) {
				if(command!=""){ command+=",";}
				command+="Notes = '"+POut.String(evaluationCriterion.Notes)+"'";
			}
			if(evaluationCriterion.ItemOrder != oldEvaluationCriterion.ItemOrder) {
				if(command!=""){ command+=",";}
				command+="ItemOrder = "+POut.Int(evaluationCriterion.ItemOrder)+"";
			}
			if(evaluationCriterion.MaxPointsPoss != oldEvaluationCriterion.MaxPointsPoss) {
				if(command!=""){ command+=",";}
				command+="MaxPointsPoss = "+POut.Float(evaluationCriterion.MaxPointsPoss)+"";
			}
			if(command==""){
				return false;
			}
			command="UPDATE evaluationcriterion SET "+command
				+" WHERE EvaluationCriterionNum = "+POut.Long(evaluationCriterion.EvaluationCriterionNum);
			Db.NonQ(command);
			return true;
		}

		///<summary>Deletes one EvaluationCriterion from the database.</summary>
		public static void Delete(long evaluationCriterionNum){
			string command="DELETE FROM evaluationcriterion "
				+"WHERE EvaluationCriterionNum = "+POut.Long(evaluationCriterionNum);
			Db.NonQ(command);
		}

	}
}