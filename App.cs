#region Namespaces
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using System.Linq;
using System.IO;
using Autodesk.Revit.DB;

#endregion

namespace TrackRemove
{
    class App : IExternalApplication
    {
        private string username;
        private string documentName; 
        private List<DeletedElement> deletedList = new List<DeletedElement>();
        private List<ElementId> addedList = new List<ElementId>();
        string tempPath = Path.GetTempPath() + "tracker\\";
        public Result OnStartup(UIControlledApplication app)
        {            
            app.ControlledApplication.DocumentChanged += OnDocumentChanged;
            app.ControlledApplication.DocumentSynchronizingWithCentral += OnDocumentSynchronizingWithCentral;
            return Result.Succeeded;
        }

        
        private void OnDocumentSynchronizingWithCentral(object sender, DocumentSynchronizingWithCentralEventArgs e)
        {
            var path = tempPath + $"{documentName.Substring(0, 4)}_{username}.csv";

            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            if (deletedList.Count>0)
            {
                using (var tw = new StreamWriter(path, true, System.Text.Encoding.GetEncoding(1251)))
                {
                    deletedList.ForEach((item) =>
                    {
                        tw.WriteLine(item.ToString());
                    });
                }
                deletedList.Clear();
                addedList.Clear();
            }            
        }

        private void OnDocumentChanged(object sender, DocumentChangedEventArgs e)
        {
            setInfos(e);

            // get deleted elements ID
            if (e.Operation == UndoOperation.TransactionCommitted)
            {
                if (e.GetTransactionNames()[0] == "Удалить выбранные" || e.GetTransactionNames()[0] == "Delete selection")
                {
                    foreach (var item in e.GetDeletedElementIds())
                    {
                        if(!addedList.Contains(item))
                            deletedList.Add(new DeletedElement(item.IntegerValue.ToString(), documentName, username));
                    }
                }
                else if(e.GetAddedElementIds().Count>0)
                {
                    addedList.AddRange(e.GetAddedElementIds().AsEnumerable());
                }
            }
            // remove deleted elements if the transaction is rolled back
            else if (e.Operation == UndoOperation.TransactionUndone)
            {
                if (e.GetTransactionNames()[0] == "Удалить выбранные" || e.GetTransactionNames()[0] == "Delete selection" && deletedList.Count > 0)
                {
                    foreach (var item in e.GetAddedElementIds())
                    {
                        try
                        {
                            var removeItem = deletedList.Where(x => x.Id == item.IntegerValue.ToString()).First();
                            deletedList.Remove(removeItem);
                        }
                        catch
                        {

                        }

                    }
                }
            }

        }

        private void setInfos(DocumentChangedEventArgs e)
        {
                var doc = e.GetDocument();
                username = doc.Application.Username;
                documentName = doc.Title;              
        }

        public Result OnShutdown(UIControlledApplication app)
        {
            app.ControlledApplication.DocumentChanged -= OnDocumentChanged;
            app.ControlledApplication.DocumentSynchronizingWithCentral -= OnDocumentSynchronizingWithCentral;
            return Result.Succeeded;
        }
    }


    struct DeletedElement
    {
        public string Id;
        public string DocName;
        public string UserName;
        private DateTime _date;

        public string Date
        {
            get
            {
                var day = (_date.Day < 9 ? "0" : "") + _date.Day;
                var month = (_date.Month < 9 ? "0" : "") + _date.Month;
                var year = _date.Year;

                return day + "-" + month + "-" + year + "," + _date.Hour + ":" + _date.Minute;
            }

        }

        public override string ToString()
        {
            return DocName + "," + UserName + "," + Id + "," + Date + "\t";
        }



        public DeletedElement(string id, string docName, string userName)
        {
            this.Id = id;
            this.DocName = docName;
            this.UserName = userName;
            this._date = DateTime.Now;
        }


    }
}



