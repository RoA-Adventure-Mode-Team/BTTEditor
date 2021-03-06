﻿using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Panels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RivalsAdventureEditor.Operations
{
    public class DeleteArticleOperation : OperationBase
    {
        Article Article { get; set; }
        Room Room { get; set; }
        int ArtIndex { get; set; }
        public override string Parameter => $"{Article.ArticleNum.ToString()}({Article.Name})";

        public DeleteArticleOperation(Project proj, Article article) : base(proj)
        {
            Article = article;
            Room = ApplicationSettings.Instance.ActiveRoom;
        }

        public override void Execute()
        {
            ArtIndex = Room.Objs.IndexOf(Article);
            Room.Objs.RemoveAt(ArtIndex);
        }

        public override void Undo()
        {
            Room.Objs.Insert(ArtIndex, Article);
        }
    }
}
