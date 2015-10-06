using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using OpenDentBusiness;

namespace OpenDental.Eclaims
{
	/// <summary>
	/// Summary description for Eclaims.
	/// </summary>
	public class Eclaims{
		/// <summary></summary>
		public Eclaims()
		{
			
		}

		///<summary>Supply a list of ClaimSendQueueItems. Called from FormClaimSend.  Can only send to one clearinghouse at a time.  Able to send just send one claim.  Cannot include Canadian.</summary>
		public static void SendBatch(List<ClaimSendQueueItem> queueItems,Clearinghouse clearhouse,EnumClaimMedType medType){
			string messageText="";
			if(clearhouse.Eformat==ElectronicClaimFormat.Canadian){
				MsgBox.Show("Eclaims","Cannot send Canadian claims as part of Eclaims.SendBatch.");
				return;
			}
			//get next batch number for this clearinghouse
			int batchNum=Clearinghouses.GetNextBatchNumber(clearhouse);
			//---------------------------------------------------------------------------------------
			//Create the claim file for this clearinghouse
			if(clearhouse.Eformat==ElectronicClaimFormat.x837D_4010
				|| clearhouse.Eformat==ElectronicClaimFormat.x837D_5010_dental
				|| clearhouse.Eformat==ElectronicClaimFormat.x837_5010_med_inst) 
			{
				messageText=x837Controller.SendBatch(queueItems,batchNum,clearhouse,medType);
			}
			else if(clearhouse.Eformat==ElectronicClaimFormat.Renaissance){
				messageText=Renaissance.SendBatch(queueItems,batchNum);
			}
			else if(clearhouse.Eformat==ElectronicClaimFormat.Dutch) {
				messageText=Dutch.SendBatch(queueItems,batchNum);
			}
			else{
				messageText="";//(ElectronicClaimFormat.None does not get sent)
			}
			if(messageText==""){//if failed to create claim file properly,
				return;//don't launch program or change claim status
			}
			//----------------------------------------------------------------------------------------
			//Launch Client Program for this clearinghouse if applicable
			if(clearhouse.CommBridge==EclaimsCommBridge.None){
				AttemptLaunch(clearhouse,batchNum);
			}
			else if(clearhouse.CommBridge==EclaimsCommBridge.WebMD){
				if(!WebMD.Launch(clearhouse,batchNum)){
					MessageBox.Show(Lan.g("Eclaims","Error sending."));
					return;
				}
			}
			else if(clearhouse.CommBridge==EclaimsCommBridge.BCBSGA){
				if(!BCBSGA.Launch(clearhouse,batchNum)){
					MessageBox.Show(Lan.g("Eclaims","Error sending."));
					return;
				}
			}
			else if(clearhouse.CommBridge==EclaimsCommBridge.Renaissance){
				AttemptLaunch(clearhouse,batchNum);
			}
			else if(clearhouse.CommBridge==EclaimsCommBridge.ClaimConnect){
				if(!ClaimConnect.Launch(clearhouse,batchNum)){
					MessageBox.Show(Lan.g("Eclaims","Error sending."));
					return;
				}
			}
			else if(clearhouse.CommBridge==EclaimsCommBridge.RECS){
				if(!RECS.Launch(clearhouse,batchNum)){
					MessageBox.Show("Claim file created, but could not launch RECS client.");
					//continue;
				}
			}
			else if(clearhouse.CommBridge==EclaimsCommBridge.Inmediata){
				if(!Inmediata.Launch(clearhouse,batchNum)){
					MessageBox.Show("Claim file created, but could not launch Inmediata client.");
					//continue;
				}
			}
			else if(clearhouse.CommBridge==EclaimsCommBridge.AOS){ // added by SPK 7/13/05
				if(!AOS.Launch(clearhouse,batchNum)){
					MessageBox.Show("Claim file created, but could not launch AOS Communicator.");
					//continue;
				}
			}
			else if(clearhouse.CommBridge==EclaimsCommBridge.PostnTrack){
				AttemptLaunch(clearhouse,batchNum);
			}
			else if(clearhouse.CommBridge==EclaimsCommBridge.MercuryDE){
				if(!MercuryDE.Launch(clearhouse,batchNum)){
					MsgBox.Show("Eclaims","Error sending.");
					return;
				}
			}
			else if(clearhouse.CommBridge==EclaimsCommBridge.ClaimX) {
				if(!ClaimX.Launch(clearhouse,batchNum)) {
					MessageBox.Show("Claim file created, but encountered an error while launching ClaimX Client.");
				}
			}
			else if(clearhouse.CommBridge==EclaimsCommBridge.EmdeonMedical) {
				if(!EmdeonMedical.Launch(clearhouse,batchNum,medType)) {
					MessageBox.Show(Lan.g("Eclaims","Error sending."));
					return;
				}
			}
			else if(clearhouse.CommBridge==EclaimsCommBridge.DentiCal) {
				if(!DentiCal.Launch(clearhouse,batchNum)) {
					return;//Error message was already shown inside of the DentiCal class.
				}
			}
			else if(clearhouse.CommBridge==EclaimsCommBridge.NHS) {
				if(!NHS.Launch(clearhouse,batchNum)) {
					MessageBox.Show(Lan.g("Eclaims","Error sending."));
					return;
				}
			}
			//----------------------------------------------------------------------------------------
			//finally, mark the claims sent. (only if not Canadian)
			EtransType etype=EtransType.ClaimSent;
			if(clearhouse.Eformat==ElectronicClaimFormat.Renaissance){
				etype=EtransType.Claim_Ren;
			}
			if(clearhouse.Eformat!=ElectronicClaimFormat.Canadian) {
				//Create the etransmessagetext that all claims in the batch will point to.
				EtransMessageText etransMsgText=new EtransMessageText();
				etransMsgText.MessageText=messageText;
				EtransMessageTexts.Insert(etransMsgText);
				for(int j=0;j<queueItems.Count;j++) {
					Etrans etrans=Etranss.SetClaimSentOrPrinted(queueItems[j].ClaimNum,queueItems[j].PatNum,clearhouse.ClearinghouseNum,etype,batchNum);
					etrans.EtransMessageTextNum=etransMsgText.EtransMessageTextNum;
					Etranss.Update(etrans);
					//Now we need to update our cache of claims to reflect the change that took place in the database above in Etranss.SetClaimSentOrPrinted()
					queueItems[j].ClaimStatus="S";
				}
			}
		}

		///<summary>If no comm bridge is selected for a clearinghouse, this launches any client program the user has entered.  We do not want to cause a rollback, so no return value.</summary>
		private static void AttemptLaunch(Clearinghouse clearhouse,int batchNum){
			if(clearhouse.ClientProgram==""){
				return;
			}
			if(!File.Exists(clearhouse.ClientProgram)){
				MessageBox.Show(clearhouse.ClientProgram+" "+Lan.g("Eclaims","does not exist."));
				return;
			}
			try{
				Process.Start(clearhouse.ClientProgram);
			}
			catch{
				MessageBox.Show(Lan.g("Eclaims","Client program could not be started.  It may already be running. You must open your client program to finish sending claims."));
			}
		}

		///<summary>Fills the missing data field on the queueItem that was passed in.  This contains all missing data on this claim.  Claim will not be allowed to be sent electronically unless this string comes back empty.</summary>
		public static void GetMissingData(ClaimSendQueueItem queueItem){//, out string warnings){
			queueItem.Warnings="";
			queueItem.MissingData="";
			Clearinghouse clearhouse=ClearinghouseL.GetClearinghouse(queueItem.ClearinghouseNum,true);//Suppress error message in case no default medical clearinghouse set.
			//this is usually just the default clearinghouse or the clearinghouse for the PayorID.
			if(clearhouse==null){
				if(queueItem.MedType==EnumClaimMedType.Dental) {
					queueItem.MissingData+="No default dental clearinghouse set.";
				}
				else {
					queueItem.MissingData+="No default medical/institutional clearinghouse set.";
				}				
				return;
			}
			#region Data Sanity Checking (for Replication)
			//Example: We had one replication customer who was able to delete an insurance plan for which was attached to a claim.
			//Imagine two replication servers, server A and server B.  An insplan is created which is not associated to any claims.
			//Both databases have a copy of the insplan.  The internet connection is lost.  On server A, a user deletes the insurance
			//plan (which is allowed because no claims are attached).  On server B, a user creates a claim with the insurance plan.
			//When the internet connection returns, the delete insplan statement is run on server B, which then creates a claim with
			//an invalid InsPlanNum on server B.  Without the checking below, the send claims window would crash for this one scenario.
			Claim claim=Claims.GetClaim(queueItem.ClaimNum);//This should always exist, because we just did a select to get the queue item.
			InsPlan insPlan=InsPlans.RefreshOne(claim.PlanNum);
			if(insPlan==null) {//Check for missing PlanNums
				queueItem.MissingData=Lans.g("Eclaims","Claim insurance plan record missing.  Please recreate claim.");
				return;
			}
			if(claim.InsSubNum2!=0) {
				InsPlan insPlan2=InsPlans.RefreshOne(claim.PlanNum2);
				if(insPlan2==null) {//Check for missing PlanNums
					queueItem.MissingData=Lans.g("Eclaims","Claim other insurance plan record missing.  Please recreate claim.");
					return;//This will let the office send other claims that passed validation without throwing an exception.
				}
			}
			#endregion Data Sanity Checking (for Replication)
			if(clearhouse.Eformat==ElectronicClaimFormat.x837D_4010){
				X837_4010.Validate(queueItem);//,out warnings);
				//return;
			}
			else if(clearhouse.Eformat==ElectronicClaimFormat.x837D_5010_dental
				|| clearhouse.Eformat==ElectronicClaimFormat.x837_5010_med_inst)
			{
				X837_5010.Validate(queueItem);//,out warnings);
				//return;
			}
			else if(clearhouse.Eformat==ElectronicClaimFormat.Renaissance){
				queueItem.MissingData=Renaissance.GetMissingData(queueItem);
				//return;
			}
			else if(clearhouse.Eformat==ElectronicClaimFormat.Canadian) {
				queueItem.MissingData=Canadian.GetMissingData(queueItem);
				//return;
			}
			else if(clearhouse.Eformat==ElectronicClaimFormat.Dutch) {
				Dutch.GetMissingData(queueItem);//,out warnings);
				//return;
			}
			//return "";
		}

	


	}
}



























