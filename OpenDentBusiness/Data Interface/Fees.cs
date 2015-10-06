using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace OpenDentBusiness{
	///<summary></summary>
	public class Fees {
		private static List<Fee> _listt;
		private static object _lockObj=new object();

		///<summary>A list of all Fees.</summary>
		public static List<Fee> Listt{
			get {
				return GetListt();
			}
			set {
				lock(_lockObj) {
					_listt=value;
				}
			}
		}

		///<summary>A list of all Fees.</summary>
		public static List<Fee> GetListt() {
			bool isListNull=false;
			lock(_lockObj) {
				if(_listt==null) {
					isListNull=true;
				}
			}
			if(isListNull) {
				Fees.RefreshCache();
			}
			List<Fee> listFees=new List<Fee>();
			lock(_lockObj) {
				for(int i=0;i<_listt.Count;i++) {
					listFees.Add(_listt[i].Copy());
				}
			}
			return listFees;
		}

		public static DataTable RefreshCache() {
			//No need to check RemotingRole; Calls GetTableRemotelyIfNeeded().
			string command="SELECT * FROM fee";
			DataTable table=Cache.GetTableRemotelyIfNeeded(MethodBase.GetCurrentMethod(),command);
			table.TableName="Fee";
			FillCache(table);
			return table;
		}

		///<summary></summary>
		public static void FillCache(DataTable table) {
			//No need to check RemotingRole; no call to db.
			Listt=Crud.FeeCrud.TableToList(table);
			/*
			Dict=new Dictionary<FeeKey,Fee>();
			FeeKey key;
			for(int i=0;i<Listt.Count;i++) {
				key=new FeeKey();
				key.codeNum=Listt[i].CodeNum;
				key.feeSchedNum=Listt[i].FeeSched;
				if(Dict.ContainsKey(key)) {
					//Older versions used to delete duplicates here
				}
				else {
					Dict.Add(key,Listt[i]);
				}
			}*/
		}

		///<summary></summary>
		public static void Update(Fee fee){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				Meth.GetVoid(MethodBase.GetCurrentMethod(),fee);
				return;
			}
			Crud.FeeCrud.Update(fee);
		}

		///<summary></summary>
		public static long Insert(Fee fee) {
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				fee.FeeNum=Meth.GetLong(MethodBase.GetCurrentMethod(),fee);
				return fee.FeeNum;
			}
			return Crud.FeeCrud.Insert(fee);
		}

		///<summary></summary>
		public static void Delete(Fee fee){
			//No need to check RemotingRole; no call to db.
			Delete(fee.FeeNum);
		}

		///<summary></summary>
		public static void Delete(long feeNum){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				Meth.GetVoid(MethodBase.GetCurrentMethod(),feeNum);
				return;
			}
			string command="DELETE FROM fee WHERE FeeNum="+feeNum;
			Db.NonQ(command);
		}

		///<summary>Returns null if no fee exists for code/feeSched combo.</summary>
		public static Fee GetFee(long codeNum,long feeSchedNum) {
			//No need to check RemotingRole; no call to db.
			if(codeNum==0){
				return null;
			}
			if(feeSchedNum==0){
				return null;
			}
			List<Fee> listFees=GetListt();
			for(int i=0;i<listFees.Count;i++) {
				if(listFees[i].CodeNum==codeNum && listFees[i].FeeSched==feeSchedNum) {
					return listFees[i];
				}
			}
			return null;
		}

		///<summary>Returns an amount if a fee has been entered.  Otherwise returns -1.  Not usually used directly.</summary>
		public static double GetAmount(long codeNum,long feeSchedNum) {
			//No need to check RemotingRole; no call to db.
			if(codeNum==0){
				return -1;
			}
			if(feeSchedNum==0){
				return -1;
			}
			if(FeeScheds.GetIsHidden(feeSchedNum)){
				return -1;//you cannot obtain fees for hidden fee schedules
			}
			List<Fee> listFees=GetListt();
			for(int i=0;i<listFees.Count;i++) {
				if(listFees[i].CodeNum==codeNum && listFees[i].FeeSched==feeSchedNum) {
					return listFees[i].Amount;
				}
			}
			return -1;//code not found
		}

		///<summary>Almost the same as GetAmount.  But never returns -1;  Returns an amount if a fee has been entered.  Returns 0 if code can't be found.
		///TODO: =js 6/19/13 There are many places where this is used to get the fee for a proc.  This results in approx 12 identical chunks of code throughout the program.
		///We need to build a method to eliminate all those identical sections.  This will prevent bugs from cropping up when these sections get out of synch.</summary>
		public static double GetAmount0(long codeNum,long feeSched) {
			//No need to check RemotingRole; no call to db.
			double retVal=GetAmount(codeNum,feeSched);
			if(retVal==-1){
				return 0;
			}
			return retVal;															 
		}

		///<summary>Gets the fee schedule from the first insplan, the patient, or the provider in that order.  Either returns a fee schedule (fk to definition.DefNum) or 0.</summary>
		public static long GetFeeSched(Patient pat,List<InsPlan> planList,List<PatPlan> patPlans,List<InsSub> subList) {
			//No need to check RemotingRole; no call to db.
			//there's not really a good place to put this function, so it's here.
			long retVal=0;
			//First, try getting the fee schedule from the insplan.
			if(PatPlans.GetInsSubNum(patPlans,1)!=0){
				InsSub SubCur=InsSubs.GetSub(PatPlans.GetInsSubNum(patPlans,1),subList);
				InsPlan PlanCur=InsPlans.GetPlan(SubCur.PlanNum,planList);
				if(PlanCur==null){
					retVal=0;
				}
				else{
					retVal=PlanCur.FeeSched;
				}
			}
			if(retVal==0){//Ins plan did not have a fee sched, check the patient.
				if(pat.FeeSched!=0){
					retVal=pat.FeeSched;
				}
				else {//Patient did not have a fee sched, check the provider.
					List<Provider> listProvs=ProviderC.GetListLong();
					if(pat.PriProv!=0 && listProvs.Count>0) {
						retVal=listProvs[Providers.GetIndexLong(pat.PriProv,listProvs)].FeeSched;//Guaranteed to work because ProviderC.ListLong has at least one provider in the list.
					}
				}
			}
			return retVal;
		}

		///<summary>A simpler version of the same function above.  The required numbers can be obtained in a fairly simple query.  Might return a 0 if the primary provider does not have a fee schedule set.</summary>
		public static long GetFeeSched(long priPlanFeeSched,long patFeeSched,long patPriProvNum) {
			//No need to check RemotingRole; no call to db.
			if(priPlanFeeSched!=0){
				return priPlanFeeSched;
			}
			if(patFeeSched!=0){
				return patFeeSched;
			}
			List<Provider> listProvs=ProviderC.GetListLong();
			return listProvs[Providers.GetIndexLong(patPriProvNum,listProvs)].FeeSched;
		}

		///<summary>Gets the fee schedule from the primary MEDICAL insurance plan, the first insurance plan, the patient, or the provider in that order.</summary>
		public static long GetMedFeeSched(Patient pat,List<InsPlan> planList,List<PatPlan> patPlans,List<InsSub> subList) {
			//No need to check RemotingRole; no call to db. ??
			long retVal = 0;
			if(PatPlans.GetInsSubNum(patPlans,1) != 0){
				//Pick the medinsplan with the ordinal closest to zero
				int planOrdinal=10; //This is a hack, but I doubt anyone would have more than 10 plans
				bool hasMedIns=false; //Keep track of whether we found a medical insurance plan, if not use dental insurance fee schedule.
				InsSub subCur;
				foreach(PatPlan patplan in patPlans){
					subCur=InsSubs.GetSub(patplan.InsSubNum,subList);
					if(patplan.Ordinal<planOrdinal && InsPlans.GetPlan(subCur.PlanNum,planList).IsMedical) {
						planOrdinal=patplan.Ordinal;
						hasMedIns=true;
					}
				}
				if(!hasMedIns) { //If this patient doesn't have medical insurance (under ordinal 10)
					return GetFeeSched(pat,planList,patPlans,subList);  //Use dental insurance fee schedule
				}
				subCur=InsSubs.GetSub(PatPlans.GetInsSubNum(patPlans,planOrdinal),subList);
				InsPlan PlanCur=InsPlans.GetPlan(subCur.PlanNum, planList);
				if (PlanCur==null){
					retVal=0;
				} 
				else {
					retVal=PlanCur.FeeSched;
				}
			}
			if (retVal==0){
				if (pat.FeeSched!=0){
					retVal=pat.FeeSched;
				} 
				else {
					if (pat.PriProv==0){
						List<Provider> listProvs=ProviderC.GetListShort();
						retVal=listProvs[0].FeeSched;
					} 
					else {
						//MessageBox.Show(Providers.GetIndex(Patients.Cur.PriProv).ToString());   
						List<Provider> listProvs=ProviderC.GetListLong();
						retVal=listProvs[Providers.GetIndexLong(pat.PriProv,listProvs)].FeeSched;
					}
				}
			}
			return retVal;
		}

		///<summary>Clears all fees from one fee schedule.  Supply the FeeSchedNum of the feeSchedule.</summary>
		public static void ClearFeeSched(long schedNum) {
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				Meth.GetVoid(MethodBase.GetCurrentMethod(),schedNum);
				return;
			}
			string command="DELETE FROM fee WHERE FeeSched="+schedNum;
			Db.NonQ(command);
		}

		///<summary>Copies any fee objects over to the new fee schedule.  Usually run ClearFeeSched first.  Be careful exactly which int's you supply.</summary>
		public static void CopyFees(long fromFeeSched,long toFeeSched) {
			//No need to check RemotingRole; no call to db.
			List<Fee> listFees=GetListt();
			if(listFees==null) {
				RefreshCache();
			}
			Fee fee;
			for(int i=0;i<listFees.Count;i++){
				if(listFees[i].FeeSched!=fromFeeSched){
					continue;
				}
				fee=listFees[i].Copy();
				fee.FeeSched=toFeeSched;
				Fees.Insert(fee);
			}
		}

		///<summary>Increases the fee schedule by percent.  Round should be the number of decimal places, either 0,1,or 2.</summary>
		public static void Increase(long feeSched,int percent,int round) {
			//No need to check RemotingRole; no call to db.
			List<Fee> listFees=GetListt();
			if(listFees==null) {
				RefreshCache();
			}
			Fee fee;
			double newVal;
			for(int i=0;i<listFees.Count;i++){
				if(listFees[i].FeeSched!=feeSched){
					continue;
				}
				if(listFees[i].Amount==0){
					continue;
				}
				fee=listFees[i].Copy();
				newVal=(double)fee.Amount*(1+(double)percent/100);
				if(round>0) {
					fee.Amount=Math.Round(newVal,round);
				}
				else {
					fee.Amount=Math.Round(newVal,MidpointRounding.AwayFromZero);
				}
				Fees.Update(fee);
			}
		}

		///<summary>schedI is the currently displayed index of the fee schedule to save to.  If an amt of -1 is passed in, then it indicates a "blank" entry which will cause deletion of any existing fee.</summary>
		public static void Import(string codeText,double amt,long feeSchedNum) {
			//No need to check RemotingRole; no call to db.
			if(!ProcedureCodes.IsValidCode(codeText)){
				return;//skip for now. Possibly insert a code in a future version.
			}
			long feeNum=GetFeeNum(ProcedureCodes.GetCodeNum(codeText),feeSchedNum);
			if(feeNum>0) {
				Delete(feeNum);
			}
			if(amt==-1) {
				//RefreshCache();
				return;
			}
			Fee fee=new Fee();
			fee.Amount=amt;
			fee.FeeSched=feeSchedNum;
			fee.CodeNum=ProcedureCodes.GetCodeNum(codeText);
			Insert(fee);
			//RefreshCache();//moved this outside the loop
		}

		///<summary>Gets the FeeNum from the database, returns 0 if none found.</summary>
		public static long GetFeeNum(long codeNum,long feeSchedNum) {
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				return Meth.GetLong(MethodBase.GetCurrentMethod(),codeNum,feeSchedNum);
			}
			string command="SELECT FeeNum FROM fee WHERE CodeNum="+POut.Long(codeNum)+" AND FeeSched="+POut.Long(feeSchedNum);
			return PIn.Long(Db.GetScalar(command));
		}

	}

	public struct FeeKey{
		public long codeNum;
		public long feeSchedNum;
	}

}