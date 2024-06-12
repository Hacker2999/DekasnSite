using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using DekasnSite.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using OfficeOpenXml;

namespace DekasnSite.Controllers
{
    public class DocumentsController : Controller
    {
        private Dekan_dbEntities db = new Dekan_dbEntities();

        // Метод для экспорта данных в файл Excel
        public ActionResult ExportToExcel()
        {
            var applications = db.Documents.ToList();

            // Задаем заголовки столбцов и свойства для включения в Excel
            var columnHeaders = new Dictionary<string, string>
    {
        { "ID_Document", "ID Document" },
        { "DocumentType", "Document Type" },
        { "CreationDate", "Creation Date" },
        { "Description", "Description" }
    };

            var propertiesToInclude = new List<string> { "ID_Document", "DocumentType", "CreationDate", "Description" };

            // Создаем файл Excel и возвращаем его как результат действия
            var fileName = "applications.xlsx";
            return GenerateExcel(applications, fileName, columnHeaders, propertiesToInclude);
        }


        // Метод для экспорта данных в файл Word
        public ActionResult ExportToWord()
        {
            var applications = db.Documents.ToList();

            // Задаем заголовки столбцов и свойства для включения в Word
            var columnHeaders = new Dictionary<string, string>
    {
        { "ID_Document", "ID Document" },
        { "DocumentType", "Document Type" },
        { "CreationDate", "Creation Date" },
        { "Description", "Description" }
    };

            var propertiesToInclude = new List<string> { "ID_Document", "DocumentType", "CreationDate", "Description" };

            // Создаем файл Word и возвращаем его как результат действия
            var fileName = "applications.docx";
            return GenerateWord(applications, fileName, columnHeaders, propertiesToInclude);
        }


        // Метод для генерации файла Excel
        private ActionResult GenerateExcel(IEnumerable<Models.Document> data, string fileName, Dictionary<string, string> columnHeaders, List<string> propertiesToInclude)
        {
            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Applications");

                // Добавляем заголовки столбцов, используя переданный словарь
                int headerRow = 1;
                int column = 1;
                foreach (var columnHeader in columnHeaders)
                {
                    worksheet.Cells[headerRow, column].Value = columnHeader.Value;
                    column++;
                }

                // Добавляем данные из свойств, указанных в списке propertiesToInclude
                int dataRow = 2;
                foreach (var item in data)
                {
                    column = 1;
                    foreach (var property in propertiesToInclude)
                    {
                        var propValue = item.GetType().GetProperty(property)?.GetValue(item, null);
                        worksheet.Cells[dataRow, column].Value = propValue != null ? propValue.ToString() : "";
                        column++;
                    }
                    dataRow++;
                }

                // Форматируем таблицу
                worksheet.Cells.AutoFitColumns();
                worksheet.Cells[headerRow, 1, dataRow - 1, columnHeaders.Count].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                worksheet.Cells[headerRow, 1, headerRow, columnHeaders.Count].Style.Font.Bold = true;

                // Переводим данные в байтовый массив и возвращаем как файл
                byte[] fileBytes = package.GetAsByteArray();
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // Метод для генерации файла Word
        private ActionResult GenerateWord(IEnumerable<Models.Document> data, string fileName, Dictionary<string, string> columnHeaders, List<string> propertiesToInclude)
        {
            MemoryStream ms = new MemoryStream();
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                Body body = mainPart.Document.AppendChild(new Body());

                // Добавляем таблицу
                Table table = new Table();
                TableProperties tableProperties = new TableProperties(
    new TableBorders(
        new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
        new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
        new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
        new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
        new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
        new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 }
    )
);
                table.AppendChild(tableProperties);

                // Добавляем строку заголовков
                TableRow headerRow = new TableRow();
                foreach (var columnHeader in columnHeaders)
                {
                    TableCell headerCell = new TableCell(new Paragraph(new Run(new Text(columnHeader.Value))));

                    headerRow.AppendChild(headerCell);
                }
                table.AppendChild(headerRow);

                // Добавляем строки с данными
                foreach (var item in data)
                {
                    TableRow row = new TableRow();
                    foreach (var property in propertiesToInclude)
                    {
                        var prop = typeof(Models.Document).GetProperty(property);
                        string value = prop.GetValue(item)?.ToString() ?? "";
                        TableCell cell = new TableCell(new Paragraph(new Run(new Text(value))));
                        row.AppendChild(cell);
                    }
                    table.AppendChild(row);
                }

                body.Append(table);

                // Сохраняем документ в поток
                wordDocument.Save();
            }

            // Возвращаем файл Word как результат действия
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }

        // GET: Documents
        public ActionResult Index(string sortOrder, string SearchString)
        {
            var documents = db.Documents.Include(d => d.Student);

            ViewBag.DocumentTypeParm = String.IsNullOrEmpty(sortOrder) ? "DocumentType_desc" : "";
            ViewBag.CreationDateParm = sortOrder == "CreationDate" ? "CreationDate_desc" : "CreationDate";
            ViewBag.DescriptionParm = sortOrder == "Description" ? "Description_desc" : "Description";
            ViewBag.StudentName = sortOrder == "StudentName" ? "StudentName_desc" : "StudentName";

            documents = from a in db.Documents
                        select a;
            if (!String.IsNullOrEmpty(SearchString))
            {
                documents = documents.Where
                    (s => s.DocumentType.Contains(SearchString)
                    || s.Description.Contains(SearchString)
                    || s.Student.Name.Contains(SearchString));
            }
            switch (sortOrder)
            {
                case "DocumentType_desc":
                    documents = documents.OrderByDescending(s => s.DocumentType);
                    break;
                case "CreationDate":
                    documents = documents.OrderBy(s => s.CreationDate);
                    break;
                case "CreationDate_desc":
                    documents = documents.OrderByDescending(s => s.CreationDate);
                    break;
                case "Description":
                    documents = documents.OrderBy(s => s.Description);
                    break;
                case "Description_desc":
                    documents = documents.OrderByDescending(s => s.Description);
                    break;
                case "StudentName":
                    documents = documents.OrderBy(s => s.Student.Name);
                    break;
                case "StudentName_desc":
                    documents = documents.OrderByDescending(s => s.Student.Name);
                    break;
                default:
                    documents = documents.OrderBy(s => s.DocumentType);
                    break;
            }

            return View(documents.ToList());
        }

        // GET: Documents/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Models.Document document = db.Documents.Find(id);
            if (document == null)
            {
                return HttpNotFound();
            }
            return View(document);
        }

        // GET: Documents/Create
        public ActionResult Create()
        {
            ViewBag.AuthorID = new SelectList(db.Students, "ID_Student", "Name");
            return View();
        }

        // POST: Documents/Create
        // Чтобы защититься от атак чрезмерной передачи данных, включите определенные свойства, для которых следует установить привязку. Дополнительные 
        // сведения см. в разделе https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID_Document,DocumentType,CreationDate,AuthorID,Description")] Models.Document document)
        {
            if (ModelState.IsValid)
            {
                db.Documents.Add(document);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.AuthorID = new SelectList(db.Students, "ID_Student", "Name", document.AuthorID);
            return View(document);
        }

        // GET: Documents/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Models.Document document = db.Documents.Find(id);
            if (document == null)
            {
                return HttpNotFound();
            }
            ViewBag.AuthorID = new SelectList(db.Students, "ID_Student", "Name", document.AuthorID);
            return View(document);
        }

        // POST: Documents/Edit/5
        // Чтобы защититься от атак чрезмерной передачи данных, включите определенные свойства, для которых следует установить привязку. Дополнительные 
        // сведения см. в разделе https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID_Document,DocumentType,CreationDate,AuthorID,Description")] Models.Document document)
        {
            if (ModelState.IsValid)
            {
                db.Entry(document).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.AuthorID = new SelectList(db.Students, "ID_Student", "Name", document.AuthorID);
            return View(document);
        }

        // GET: Documents/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Models.Document document = db.Documents.Find(id);
            if (document == null)
            {
                return HttpNotFound();
            }
            return View(document);
        }

        // POST: Documents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Models.Document document = db.Documents.Find(id);
            db.Documents.Remove(document);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
