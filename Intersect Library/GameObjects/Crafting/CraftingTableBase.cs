﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using Intersect.GameObjects.Crafting;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.GameObjects
{
    public class CraftingTableBase : DatabaseObject<CraftingTableBase>
    {
        [JsonIgnore]
        [Column("Crafts")]
        public string CraftsJson
        {
            get => JsonConvert.SerializeObject(Crafts, Formatting.None);
            protected set => Crafts = JsonConvert.DeserializeObject<DbList<CraftBase>>(value);
        }
        [NotMapped]
        public DbList<CraftBase> Crafts = new DbList<CraftBase>();



        [JsonConstructor]
        public CraftingTableBase(int index) : base(index)
        {
            Name = "New Table";
        }


        //Parameterless constructor for EF
        public CraftingTableBase()
        {
            Name = "New Table";
        }
    }
}
