//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан по шаблону.
//
//     Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//     Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DekasnSite.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Document
    {
        public int ID_Document { get; set; }
        public string DocumentType { get; set; }
        public Nullable<System.DateTime> CreationDate { get; set; }
        public Nullable<int> AuthorID { get; set; }
        public string Description { get; set; }
    
        public virtual Student Student { get; set; }
    }
}