//This file is automatically generated.
//Do not attempt to make changes to this file because the changes will be erased and overwritten.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;

namespace OpenDentBusiness.Crud{
	public class ProgramCrud {
		///<summary>Gets one Program object from the database using the primary key.  Returns null if not found.</summary>
		public static Program SelectOne(long programNum){
			string command="SELECT * FROM program "
				+"WHERE ProgramNum = "+POut.Long(programNum);
			List<Program> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets one Program object from the database using a query.</summary>
		public static Program SelectOne(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<Program> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets a list of Program objects from the database using a query.</summary>
		public static List<Program> SelectMany(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<Program> list=TableToList(Db.GetTable(command));
			return list;
		}

		///<summary>Converts a DataTable to a list of objects.</summary>
		public static List<Program> TableToList(DataTable table){
			List<Program> retVal=new List<Program>();
			Program program;
			for(int i=0;i<table.Rows.Count;i++) {
				program=new Program();
				program.ProgramNum   = PIn.Long  (table.Rows[i]["ProgramNum"].ToString());
				program.ProgName     = PIn.String(table.Rows[i]["ProgName"].ToString());
				program.ProgDesc     = PIn.String(table.Rows[i]["ProgDesc"].ToString());
				program.Enabled      = PIn.Bool  (table.Rows[i]["Enabled"].ToString());
				program.Path         = PIn.String(table.Rows[i]["Path"].ToString());
				program.CommandLine  = PIn.String(table.Rows[i]["CommandLine"].ToString());
				program.Note         = PIn.String(table.Rows[i]["Note"].ToString());
				program.PluginDllName= PIn.String(table.Rows[i]["PluginDllName"].ToString());
				retVal.Add(program);
			}
			return retVal;
		}

		///<summary>Inserts one Program into the database.  Returns the new priKey.</summary>
		public static long Insert(Program program){
			if(DataConnection.DBtype==DatabaseType.Oracle) {
				program.ProgramNum=DbHelper.GetNextOracleKey("program","ProgramNum");
				int loopcount=0;
				while(loopcount<100){
					try {
						return Insert(program,true);
					}
					catch(Oracle.DataAccess.Client.OracleException ex){
						if(ex.Number==1 && ex.Message.ToLower().Contains("unique constraint") && ex.Message.ToLower().Contains("violated")){
							program.ProgramNum++;
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
				return Insert(program,false);
			}
		}

		///<summary>Inserts one Program into the database.  Provides option to use the existing priKey.</summary>
		public static long Insert(Program program,bool useExistingPK){
			if(!useExistingPK && PrefC.RandomKeys) {
				program.ProgramNum=ReplicationServers.GetKey("program","ProgramNum");
			}
			string command="INSERT INTO program (";
			if(useExistingPK || PrefC.RandomKeys) {
				command+="ProgramNum,";
			}
			command+="ProgName,ProgDesc,Enabled,Path,CommandLine,Note,PluginDllName) VALUES(";
			if(useExistingPK || PrefC.RandomKeys) {
				command+=POut.Long(program.ProgramNum)+",";
			}
			command+=
				 "'"+POut.String(program.ProgName)+"',"
				+"'"+POut.String(program.ProgDesc)+"',"
				+    POut.Bool  (program.Enabled)+","
				+"'"+POut.String(program.Path)+"',"
				+"'"+POut.String(program.CommandLine)+"',"
				+"'"+POut.String(program.Note)+"',"
				+"'"+POut.String(program.PluginDllName)+"')";
			if(useExistingPK || PrefC.RandomKeys) {
				Db.NonQ(command);
			}
			else {
				program.ProgramNum=Db.NonQ(command,true);
			}
			return program.ProgramNum;
		}

		///<summary>Updates one Program in the database.</summary>
		public static void Update(Program program){
			string command="UPDATE program SET "
				+"ProgName     = '"+POut.String(program.ProgName)+"', "
				+"ProgDesc     = '"+POut.String(program.ProgDesc)+"', "
				+"Enabled      =  "+POut.Bool  (program.Enabled)+", "
				+"Path         = '"+POut.String(program.Path)+"', "
				+"CommandLine  = '"+POut.String(program.CommandLine)+"', "
				+"Note         = '"+POut.String(program.Note)+"', "
				+"PluginDllName= '"+POut.String(program.PluginDllName)+"' "
				+"WHERE ProgramNum = "+POut.Long(program.ProgramNum);
			Db.NonQ(command);
		}

		///<summary>Updates one Program in the database.  Uses an old object to compare to, and only alters changed fields.  This prevents collisions and concurrency problems in heavily used tables.  Returns true if an update occurred.</summary>
		public static bool Update(Program program,Program oldProgram){
			string command="";
			if(program.ProgName != oldProgram.ProgName) {
				if(command!=""){ command+=",";}
				command+="ProgName = '"+POut.String(program.ProgName)+"'";
			}
			if(program.ProgDesc != oldProgram.ProgDesc) {
				if(command!=""){ command+=",";}
				command+="ProgDesc = '"+POut.String(program.ProgDesc)+"'";
			}
			if(program.Enabled != oldProgram.Enabled) {
				if(command!=""){ command+=",";}
				command+="Enabled = "+POut.Bool(program.Enabled)+"";
			}
			if(program.Path != oldProgram.Path) {
				if(command!=""){ command+=",";}
				command+="Path = '"+POut.String(program.Path)+"'";
			}
			if(program.CommandLine != oldProgram.CommandLine) {
				if(command!=""){ command+=",";}
				command+="CommandLine = '"+POut.String(program.CommandLine)+"'";
			}
			if(program.Note != oldProgram.Note) {
				if(command!=""){ command+=",";}
				command+="Note = '"+POut.String(program.Note)+"'";
			}
			if(program.PluginDllName != oldProgram.PluginDllName) {
				if(command!=""){ command+=",";}
				command+="PluginDllName = '"+POut.String(program.PluginDllName)+"'";
			}
			if(command==""){
				return false;
			}
			command="UPDATE program SET "+command
				+" WHERE ProgramNum = "+POut.Long(program.ProgramNum);
			Db.NonQ(command);
			return true;
		}

		///<summary>Deletes one Program from the database.</summary>
		public static void Delete(long programNum){
			string command="DELETE FROM program "
				+"WHERE ProgramNum = "+POut.Long(programNum);
			Db.NonQ(command);
		}

	}
}