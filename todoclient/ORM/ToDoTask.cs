﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;

namespace todoclient.ORM
{
    public class ToDoTask
    {
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        
        public string Name { get; set; }

        public bool IsCompleted { get; set; }

        public virtual User User { get; set; }

    }
}