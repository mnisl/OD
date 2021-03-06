//This file is automatically generated.
//Do not attempt to make changes to this file because the changes will be erased and overwritten.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;

namespace OpenDentBusiness.Crud{
	public class EvaluationCrud {
		///<summary>Gets one Evaluation object from the database using the primary key.  Returns null if not found.</summary>
		public static Evaluation SelectOne(long evaluationNum){
			string command="SELECT * FROM evaluation "
				+"WHERE EvaluationNum = "+POut.Long(evaluationNum);
			List<Evaluation> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets one Evaluation object from the database using a query.</summary>
		public static Evaluation SelectOne(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<Evaluation> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets a list of Evaluation objects from the database using a query.</summary>
		public static List<Evaluation> SelectMany(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<Evaluation> list=TableToList(Db.GetTable(command));
			return list;
		}

		///<summary>Converts a DataTable to a list of objects.</summary>
		public static List<Evaluation> TableToList(DataTable table){
			List<Evaluation> retVal=new List<Evaluation>();
			Evaluation evaluation;
			for(int i=0;i<table.Rows.Count;i++) {
				evaluation=new Evaluation();
				evaluation.EvaluationNum      = PIn.Long  (table.Rows[i]["EvaluationNum"].ToString());
				evaluation.InstructNum        = PIn.Long  (table.Rows[i]["InstructNum"].ToString());
				evaluation.StudentNum         = PIn.Long  (table.Rows[i]["StudentNum"].ToString());
				evaluation.SchoolCourseNum    = PIn.Long  (table.Rows[i]["SchoolCourseNum"].ToString());
				evaluation.EvalTitle          = PIn.String(table.Rows[i]["EvalTitle"].ToString());
				evaluation.DateEval           = PIn.Date  (table.Rows[i]["DateEval"].ToString());
				evaluation.GradingScaleNum    = PIn.Long  (table.Rows[i]["GradingScaleNum"].ToString());
				evaluation.OverallGradeShowing= PIn.String(table.Rows[i]["OverallGradeShowing"].ToString());
				evaluation.OverallGradeNumber = PIn.Float (table.Rows[i]["OverallGradeNumber"].ToString());
				evaluation.Notes              = PIn.String(table.Rows[i]["Notes"].ToString());
				retVal.Add(evaluation);
			}
			return retVal;
		}

		///<summary>Inserts one Evaluation into the database.  Returns the new priKey.</summary>
		public static long Insert(Evaluation evaluation){
			if(DataConnection.DBtype==DatabaseType.Oracle) {
				evaluation.EvaluationNum=DbHelper.GetNextOracleKey("evaluation","EvaluationNum");
				int loopcount=0;
				while(loopcount<100){
					try {
						return Insert(evaluation,true);
					}
					catch(Oracle.DataAccess.Client.OracleException ex){
						if(ex.Number==1 && ex.Message.ToLower().Contains("unique constraint") && ex.Message.ToLower().Contains("violated")){
							evaluation.EvaluationNum++;
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
				return Insert(evaluation,false);
			}
		}

		///<summary>Inserts one Evaluation into the database.  Provides option to use the existing priKey.</summary>
		public static long Insert(Evaluation evaluation,bool useExistingPK){
			if(!useExistingPK && PrefC.RandomKeys) {
				evaluation.EvaluationNum=ReplicationServers.GetKey("evaluation","EvaluationNum");
			}
			string command="INSERT INTO evaluation (";
			if(useExistingPK || PrefC.RandomKeys) {
				command+="EvaluationNum,";
			}
			command+="InstructNum,StudentNum,SchoolCourseNum,EvalTitle,DateEval,GradingScaleNum,OverallGradeShowing,OverallGradeNumber,Notes) VALUES(";
			if(useExistingPK || PrefC.RandomKeys) {
				command+=POut.Long(evaluation.EvaluationNum)+",";
			}
			command+=
				     POut.Long  (evaluation.InstructNum)+","
				+    POut.Long  (evaluation.StudentNum)+","
				+    POut.Long  (evaluation.SchoolCourseNum)+","
				+"'"+POut.String(evaluation.EvalTitle)+"',"
				+    POut.Date  (evaluation.DateEval)+","
				+    POut.Long  (evaluation.GradingScaleNum)+","
				+"'"+POut.String(evaluation.OverallGradeShowing)+"',"
				+    POut.Float (evaluation.OverallGradeNumber)+","
				+"'"+POut.String(evaluation.Notes)+"')";
			if(useExistingPK || PrefC.RandomKeys) {
				Db.NonQ(command);
			}
			else {
				evaluation.EvaluationNum=Db.NonQ(command,true);
			}
			return evaluation.EvaluationNum;
		}

		///<summary>Updates one Evaluation in the database.</summary>
		public static void Update(Evaluation evaluation){
			string command="UPDATE evaluation SET "
				+"InstructNum        =  "+POut.Long  (evaluation.InstructNum)+", "
				+"StudentNum         =  "+POut.Long  (evaluation.StudentNum)+", "
				+"SchoolCourseNum    =  "+POut.Long  (evaluation.SchoolCourseNum)+", "
				+"EvalTitle          = '"+POut.String(evaluation.EvalTitle)+"', "
				+"DateEval           =  "+POut.Date  (evaluation.DateEval)+", "
				+"GradingScaleNum    =  "+POut.Long  (evaluation.GradingScaleNum)+", "
				+"OverallGradeShowing= '"+POut.String(evaluation.OverallGradeShowing)+"', "
				+"OverallGradeNumber =  "+POut.Float (evaluation.OverallGradeNumber)+", "
				+"Notes              = '"+POut.String(evaluation.Notes)+"' "
				+"WHERE EvaluationNum = "+POut.Long(evaluation.EvaluationNum);
			Db.NonQ(command);
		}

		///<summary>Updates one Evaluation in the database.  Uses an old object to compare to, and only alters changed fields.  This prevents collisions and concurrency problems in heavily used tables.  Returns true if an update occurred.</summary>
		public static bool Update(Evaluation evaluation,Evaluation oldEvaluation){
			string command="";
			if(evaluation.InstructNum != oldEvaluation.InstructNum) {
				if(command!=""){ command+=",";}
				command+="InstructNum = "+POut.Long(evaluation.InstructNum)+"";
			}
			if(evaluation.StudentNum != oldEvaluation.StudentNum) {
				if(command!=""){ command+=",";}
				command+="StudentNum = "+POut.Long(evaluation.StudentNum)+"";
			}
			if(evaluation.SchoolCourseNum != oldEvaluation.SchoolCourseNum) {
				if(command!=""){ command+=",";}
				command+="SchoolCourseNum = "+POut.Long(evaluation.SchoolCourseNum)+"";
			}
			if(evaluation.EvalTitle != oldEvaluation.EvalTitle) {
				if(command!=""){ command+=",";}
				command+="EvalTitle = '"+POut.String(evaluation.EvalTitle)+"'";
			}
			if(evaluation.DateEval != oldEvaluation.DateEval) {
				if(command!=""){ command+=",";}
				command+="DateEval = "+POut.Date(evaluation.DateEval)+"";
			}
			if(evaluation.GradingScaleNum != oldEvaluation.GradingScaleNum) {
				if(command!=""){ command+=",";}
				command+="GradingScaleNum = "+POut.Long(evaluation.GradingScaleNum)+"";
			}
			if(evaluation.OverallGradeShowing != oldEvaluation.OverallGradeShowing) {
				if(command!=""){ command+=",";}
				command+="OverallGradeShowing = '"+POut.String(evaluation.OverallGradeShowing)+"'";
			}
			if(evaluation.OverallGradeNumber != oldEvaluation.OverallGradeNumber) {
				if(command!=""){ command+=",";}
				command+="OverallGradeNumber = "+POut.Float(evaluation.OverallGradeNumber)+"";
			}
			if(evaluation.Notes != oldEvaluation.Notes) {
				if(command!=""){ command+=",";}
				command+="Notes = '"+POut.String(evaluation.Notes)+"'";
			}
			if(command==""){
				return false;
			}
			command="UPDATE evaluation SET "+command
				+" WHERE EvaluationNum = "+POut.Long(evaluation.EvaluationNum);
			Db.NonQ(command);
			return true;
		}

		///<summary>Deletes one Evaluation from the database.</summary>
		public static void Delete(long evaluationNum){
			string command="DELETE FROM evaluation "
				+"WHERE EvaluationNum = "+POut.Long(evaluationNum);
			Db.NonQ(command);
		}

	}
}