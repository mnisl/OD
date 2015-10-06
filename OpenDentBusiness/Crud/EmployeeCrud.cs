//This file is automatically generated.
//Do not attempt to make changes to this file because the changes will be erased and overwritten.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;

namespace OpenDentBusiness.Crud{
	public class EmployeeCrud {
		///<summary>Gets one Employee object from the database using the primary key.  Returns null if not found.</summary>
		public static Employee SelectOne(long employeeNum){
			string command="SELECT * FROM employee "
				+"WHERE EmployeeNum = "+POut.Long(employeeNum);
			List<Employee> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets one Employee object from the database using a query.</summary>
		public static Employee SelectOne(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<Employee> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets a list of Employee objects from the database using a query.</summary>
		public static List<Employee> SelectMany(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<Employee> list=TableToList(Db.GetTable(command));
			return list;
		}

		///<summary>Converts a DataTable to a list of objects.</summary>
		public static List<Employee> TableToList(DataTable table){
			List<Employee> retVal=new List<Employee>();
			Employee employee;
			for(int i=0;i<table.Rows.Count;i++) {
				employee=new Employee();
				employee.EmployeeNum= PIn.Long  (table.Rows[i]["EmployeeNum"].ToString());
				employee.LName      = PIn.String(table.Rows[i]["LName"].ToString());
				employee.FName      = PIn.String(table.Rows[i]["FName"].ToString());
				employee.MiddleI    = PIn.String(table.Rows[i]["MiddleI"].ToString());
				employee.IsHidden   = PIn.Bool  (table.Rows[i]["IsHidden"].ToString());
				employee.ClockStatus= PIn.String(table.Rows[i]["ClockStatus"].ToString());
				employee.PhoneExt   = PIn.Int   (table.Rows[i]["PhoneExt"].ToString());
				employee.PayrollID  = PIn.String(table.Rows[i]["PayrollID"].ToString());
				retVal.Add(employee);
			}
			return retVal;
		}

		///<summary>Inserts one Employee into the database.  Returns the new priKey.</summary>
		public static long Insert(Employee employee){
			if(DataConnection.DBtype==DatabaseType.Oracle) {
				employee.EmployeeNum=DbHelper.GetNextOracleKey("employee","EmployeeNum");
				int loopcount=0;
				while(loopcount<100){
					try {
						return Insert(employee,true);
					}
					catch(Oracle.DataAccess.Client.OracleException ex){
						if(ex.Number==1 && ex.Message.ToLower().Contains("unique constraint") && ex.Message.ToLower().Contains("violated")){
							employee.EmployeeNum++;
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
				return Insert(employee,false);
			}
		}

		///<summary>Inserts one Employee into the database.  Provides option to use the existing priKey.</summary>
		public static long Insert(Employee employee,bool useExistingPK){
			if(!useExistingPK && PrefC.RandomKeys) {
				employee.EmployeeNum=ReplicationServers.GetKey("employee","EmployeeNum");
			}
			string command="INSERT INTO employee (";
			if(useExistingPK || PrefC.RandomKeys) {
				command+="EmployeeNum,";
			}
			command+="LName,FName,MiddleI,IsHidden,ClockStatus,PhoneExt,PayrollID) VALUES(";
			if(useExistingPK || PrefC.RandomKeys) {
				command+=POut.Long(employee.EmployeeNum)+",";
			}
			command+=
				 "'"+POut.String(employee.LName)+"',"
				+"'"+POut.String(employee.FName)+"',"
				+"'"+POut.String(employee.MiddleI)+"',"
				+    POut.Bool  (employee.IsHidden)+","
				+"'"+POut.String(employee.ClockStatus)+"',"
				+    POut.Int   (employee.PhoneExt)+","
				+"'"+POut.String(employee.PayrollID)+"')";
			if(useExistingPK || PrefC.RandomKeys) {
				Db.NonQ(command);
			}
			else {
				employee.EmployeeNum=Db.NonQ(command,true);
			}
			return employee.EmployeeNum;
		}

		///<summary>Updates one Employee in the database.</summary>
		public static void Update(Employee employee){
			string command="UPDATE employee SET "
				+"LName      = '"+POut.String(employee.LName)+"', "
				+"FName      = '"+POut.String(employee.FName)+"', "
				+"MiddleI    = '"+POut.String(employee.MiddleI)+"', "
				+"IsHidden   =  "+POut.Bool  (employee.IsHidden)+", "
				+"ClockStatus= '"+POut.String(employee.ClockStatus)+"', "
				+"PhoneExt   =  "+POut.Int   (employee.PhoneExt)+", "
				+"PayrollID  = '"+POut.String(employee.PayrollID)+"' "
				+"WHERE EmployeeNum = "+POut.Long(employee.EmployeeNum);
			Db.NonQ(command);
		}

		///<summary>Updates one Employee in the database.  Uses an old object to compare to, and only alters changed fields.  This prevents collisions and concurrency problems in heavily used tables.  Returns true if an update occurred.</summary>
		public static bool Update(Employee employee,Employee oldEmployee){
			string command="";
			if(employee.LName != oldEmployee.LName) {
				if(command!=""){ command+=",";}
				command+="LName = '"+POut.String(employee.LName)+"'";
			}
			if(employee.FName != oldEmployee.FName) {
				if(command!=""){ command+=",";}
				command+="FName = '"+POut.String(employee.FName)+"'";
			}
			if(employee.MiddleI != oldEmployee.MiddleI) {
				if(command!=""){ command+=",";}
				command+="MiddleI = '"+POut.String(employee.MiddleI)+"'";
			}
			if(employee.IsHidden != oldEmployee.IsHidden) {
				if(command!=""){ command+=",";}
				command+="IsHidden = "+POut.Bool(employee.IsHidden)+"";
			}
			if(employee.ClockStatus != oldEmployee.ClockStatus) {
				if(command!=""){ command+=",";}
				command+="ClockStatus = '"+POut.String(employee.ClockStatus)+"'";
			}
			if(employee.PhoneExt != oldEmployee.PhoneExt) {
				if(command!=""){ command+=",";}
				command+="PhoneExt = "+POut.Int(employee.PhoneExt)+"";
			}
			if(employee.PayrollID != oldEmployee.PayrollID) {
				if(command!=""){ command+=",";}
				command+="PayrollID = '"+POut.String(employee.PayrollID)+"'";
			}
			if(command==""){
				return false;
			}
			command="UPDATE employee SET "+command
				+" WHERE EmployeeNum = "+POut.Long(employee.EmployeeNum);
			Db.NonQ(command);
			return true;
		}

		///<summary>Deletes one Employee from the database.</summary>
		public static void Delete(long employeeNum){
			string command="DELETE FROM employee "
				+"WHERE EmployeeNum = "+POut.Long(employeeNum);
			Db.NonQ(command);
		}

	}
}