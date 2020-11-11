using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Panels;
using RivalsAdventureEditor.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;

namespace RivalsAdventureEditor.Commands
{
    class PasteCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return Clipboard.ContainsData("Article");
        }

        public void Execute(object parameter)
        {
            if (Clipboard.ContainsData("Article"))
            {
                var data = Clipboard.GetData("Article");
                if (data is string text)
                {
                    var serializer = new JsonSerializer();
                    using (var reader = new StringReader(text))
                    {
                        JObject obj = JObject.Load(new JsonTextReader(reader));
                        Obj article;
                        ArticleType type = (ArticleType)obj.Value<int>("Article");
                        switch (type)
                        {
                            case ArticleType.Terrain:
                                article = new Terrain();
                                break;
                            case ArticleType.Zone:
                                article = new Zone();
                                break;
                            case ArticleType.Target:
                                article = new Target();
                                break;
                            default:
                                article = new Obj();
                                break;
                        }
                        serializer.Populate(obj.CreateReader(), article);
                        article.X += 1;
                        article.Y += 1;
                        var sb = new StringBuilder();
                        using (var writer = new StringWriter(sb))
                        {
                            serializer.Serialize(writer, article);
                            Clipboard.SetData("Article", sb.ToString());
                        }
                        var op = new CreateArticleOperation(ApplicationSettings.Instance.ActiveProject, article);
                        ApplicationSettings.Instance.ActiveProject.ExecuteOp(op);
                    }
                }
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
