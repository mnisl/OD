using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace OpenDentBusiness{
	///<summary></summary>
	public class RepeatCharges {
		///<summary>Gets a list of all RepeatCharges for a given patient.  Supply 0 to get a list for all patients.</summary>
		public static RepeatCharge[] Refresh(long patNum) {
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				return Meth.GetObject<RepeatCharge[]>(MethodBase.GetCurrentMethod(),patNum);
			}
			string command="SELECT * FROM repeatcharge";
			if(patNum!=0) {
				command+=" WHERE PatNum = "+POut.Long(patNum);
			}
			command+=" ORDER BY DateStart";
			return Crud.RepeatChargeCrud.SelectMany(command).ToArray();
		}	

		///<summary></summary>
		public static void Update(RepeatCharge charge){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				Meth.GetVoid(MethodBase.GetCurrentMethod(),charge);
				return;
			}
			Crud.RepeatChargeCrud.Update(charge);
		}

		///<summary></summary>
		public static long Insert(RepeatCharge charge) {
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				charge.RepeatChargeNum=Meth.GetLong(MethodBase.GetCurrentMethod(),charge);
				return charge.RepeatChargeNum;
			}
			return Crud.RepeatChargeCrud.Insert(charge);
		}

		///<summary>Called from FormRepeatCharge.</summary>
		public static void Delete(RepeatCharge charge){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				Meth.GetVoid(MethodBase.GetCurrentMethod(),charge);
				return;
			}
			string command="UPDATE procedurelog SET RepeatChargeNum=0 WHERE RepeatChargeNum="+POut.Long(charge.RepeatChargeNum);
			Db.NonQ(command);
			command="DELETE FROM repeatcharge WHERE RepeatChargeNum ="+POut.Long(charge.RepeatChargeNum);
			Db.NonQ(command);
		}

		///<summary>Used in FormRepeatChargesUpdate to get a list of the dates of procedures that have the proccode and patnum specified.</summary>
		public static ArrayList GetDates(long codeNum,long patNum) {
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				return Meth.GetObject<ArrayList>(MethodBase.GetCurrentMethod(),codeNum,patNum);
			}
			ArrayList retVal=new ArrayList();
			string command="SELECT ProcDate FROM procedurelog "
				+"WHERE PatNum="+POut.Long(patNum)
				+" AND CodeNum="+POut.Long(codeNum)
				+" AND ProcStatus=2";//complete
			DataTable table=Db.GetTable(command);
			for(int i=0;i<table.Rows.Count;i++){
				retVal.Add(PIn.Date(table.Rows[i][0].ToString()));
			}
			return retVal;
		}

		///<summary>For internal use only.  Returns the eRx repeating charges on the specified customer account.  The NPI does not have its own field, it is stored in the repeating charge note.</summary>
		public static List<RepeatCharge> GetForErx(long patNum) {
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				return Meth.GetObject<List<RepeatCharge>>(MethodBase.GetCurrentMethod(),patNum);
			}
			//Does not need to be Oracle compatible because this is an internal tool only.
			string command="SELECT * FROM repeatcharge "
				+"WHERE PatNum="+POut.Long(patNum)+" AND ProcCode REGEXP '^Z[0-9]{3,}$'";
			return Crud.RepeatChargeCrud.SelectMany(command);
		}
		

		


	}

	

	


}










