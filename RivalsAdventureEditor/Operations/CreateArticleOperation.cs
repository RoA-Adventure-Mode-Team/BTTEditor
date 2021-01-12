using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Panels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RivalsAdventureEditor.Operations
{
    public class CreateArticleOperation : OperationBase
    {
        Article Article { get; set; }
        Room Room { get; set; }
        public override string Parameter => $"{Article.ArticleNum.ToString()}({Article.Name})";
        public CreateArticleOperation(Project project, Article article) : base(project)
        {
            Article = article;
            Room = ApplicationSettings.Instance.ActiveRoom;
        }

        public override void Execute()
        {
            Room.Objs.Add(Article);
            if (ApplicationSettings.Instance.ActiveRoom == Room)
            {
                RoomEditor.Instance.SelectedObj = Article;
                RoomEditor.Instance.SelectedPath = -1;
            }
        }

        public override void Undo()
        {
            Room.Objs.Remove(Article);
            if (ApplicationSettings.Instance.ActiveRoom == Room)
            {
                RoomEditor.Instance.SelectedObj = null;
                RoomEditor.Instance.SelectedPath = -1;
            }
        }
    }
}
