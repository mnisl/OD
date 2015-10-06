using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing.Printing;

using OpenDentBusiness;
namespace OpenDental
{
    class XPSPrinterController : StandardPrintController
    {
        string _filename;
        PrintSituation _sit;
        Patient _patient;
        bool _isPrintToFile;
        public bool SetReadOnly = true;
        public bool ShowAfterPrint = true;

        public XPSPrinterController(string filename)
        {
            this.FileName = filename;
        }
        public XPSPrinterController(PrintSituation sit, long patNum)
        {
            _sit = sit;
            _patient = Patients.GetPat(patNum);
            string patfolder = ImageStore.GetPatientFolder(_patient, ImageStore.GetPreferredAtoZpath());
            this.FileName = Path.Combine(patfolder, string.Format("{0}.{1:yyyy.MM.dd.HH.mm.ss}.xps", sit, DateTime.Now));
        }

        public string FileName
        {
            get { return _filename; }
            private set { _filename = value; }
        }
        public override void OnStartPrint(PrintDocument document, PrintEventArgs e)
        {
            _isPrintToFile = document.PrinterSettings.PrintToFile;
            document.PrinterSettings.PrintToFile = true;
            document.PrinterSettings.PrintFileName = FileName;
            if(SetReadOnly)
                document.Disposed += document_Disposed;
            base.OnStartPrint(document, e);
        }

        public override void OnEndPrint(PrintDocument document, PrintEventArgs e)
        {
            base.OnEndPrint(document, e);
            document.PrinterSettings.PrintToFile = _isPrintToFile;

            if (ShowAfterPrint)
                System.Diagnostics.Process.Start(this.FileName);

            Document doc = new Document();
            doc.DateCreated = File.GetLastWriteTime(this.FileName);
            if (_sit == PrintSituation.Claim)
            {
                doc.DocCategory = DefC.GetByExactName(DefCat.ImageCats, "EClaims");
                if (doc.DocCategory == 0)
                {
                    Def d = new Def() { Category = DefCat.ImageCats, ItemName = "EClaims" };
                    doc.DocCategory = Defs.Insert(d);
                    DataValid.SetInvalid(InvalidType.Defs);
                }
            }
            else
            {
                doc.DocCategory = DefC.GetList(DefCat.ImageCats)[0].DefNum;//First category.
            }
            doc.FileName = Path.GetFileName(this.FileName);
            doc.Description = doc.FileName;
            doc.PatNum = _patient.PatNum;
            Documents.Insert(doc);
        }

        void document_Disposed(object sender, EventArgs e)
        {
            if (SetReadOnly)
            {
                try
                {
                    FileAttributes attrs = File.GetAttributes(this.FileName);
                    if (!attrs.HasFlag(FileAttributes.ReadOnly))
                    {
                        attrs |= FileAttributes.ReadOnly;
                        File.SetAttributes(this.FileName, attrs);
                    }
                }
                catch
                {
                }
            }
        }
    }    
}
