using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using todoclient.DAL;
using ToDoClient.Concrete;
using ToDoClient.Helpers;
using ToDoClient.Models;

namespace ToDoClient.Services
{
    public class SyncService : IDisposable
    {
        private readonly int maxRequestCountBeforeSync;

        private readonly ToDoService todoService;
        private readonly TempDBEntities dbEntities;


        private static readonly object syncObject;

        private BlockingCollection<CommunicationMessage> requestsList; 
        private bool syncToken;

        public CancellationTokenSource CancellationToken { get; set; }

        public SyncService(int maxRequestCountBeforeSync = 10)
        {
            this.maxRequestCountBeforeSync = maxRequestCountBeforeSync;

            syncToken = false;

            var cookie = HttpContext.Current.Request.Cookies["syncToken"];

            if (ReferenceEquals(cookie, null) || !bool.TryParse(cookie.Value, out syncToken))
            {
                cookie = new HttpCookie("syncToken", syncToken.ToString())
                {
                    Expires = DateTime.Today.AddMonths(1)
                };

                HttpContext.Current.Response.SetCookie(cookie);
            }

            todoService = new ToDoService();
            dbEntities = new TempDBEntities();
            requestsList = new BlockingCollection<CommunicationMessage>(maxRequestCountBeforeSync);
        }

        public async Task<IList<ToDoItemViewModel>> GetToDoItemsAsync(int userId)
        {

            var toDos = dbEntities.ToDoTask.Where(item => item.UserId == userId).ToList();

            if (toDos.Count == 0 || IsSyncNeeded())
            {
                List<ToDoItemViewModel>  response = await Task.Run(() => todoService.GetItems(userId).ToList());

                foreach (var item in response)
                {
                    var toDoItem = toDos.FirstOrDefault(toDo => toDo.Name.Trim().Equals(item.Name.Trim(),StringComparison.InvariantCultureIgnoreCase));

                    if (!ReferenceEquals(toDoItem, null))
                    {
                        toDoItem.Name = item.Name.Trim().Substring(0, item.Name.LastIndexOf(",",StringComparison.InvariantCultureIgnoreCase) + 1) + item.ToDoId;

                        dbEntities.Entry(toDoItem).State = EntityState.Modified;
                    }

                    if(!toDos.Any(toDo => toDo.Name.Trim().Substring(0, toDo.Name.Trim().LastIndexOf(",", StringComparison.InvariantCultureIgnoreCase))
                                            .Equals(item.Name.Trim().Substring(0, item.Name.Trim().LastIndexOf(",", StringComparison.InvariantCultureIgnoreCase)))))
                    {
                        dbEntities.ToDoTask.Add(item.ToOrmEntity());                      
                    }
                                       
                }

                dbEntities.SaveChanges();

                syncToken = true;

                var cookie = new HttpCookie("syncToken", syncToken.ToString())
                {
                    Expires = DateTime.Today.AddMonths(1)
                };

                HttpContext.Current.Response.SetCookie(cookie);
            }
          
            //ToDo: Add exeption handling           

            return dbEntities.ToDoTask.Where(item => item.UserId == userId).ToList().Select(item => item.ToUIEntity()).ToList();
        }

        public void AddToDoItem(ToDoItemViewModel toDoItem)
        {
            toDoItem.Name += "," + Guid.NewGuid();
            
            dbEntities.ToDoTask.Add(toDoItem.ToOrmEntity());
            dbEntities.SaveChanges();
            //ToDo: Add exeption handling

            AddToRequestsList(toDoItem, Operation.Add);
        }

        public void UpdateToDoItem(ToDoItemViewModel toDoItem)
        {
            /*CommunicationMessage request = requestsList.FirstOrDefault(
                msg => msg.Operation.Equals(Operation.Add) && msg.ToDoItem.Equals(toDoItem));

            if (!ReferenceEquals(request, null))
            {
                lock (syncObject)
                {
                    request.ToDoItem = toDoItem;
                }
                
                return;
            }*/

            var toDoTask = dbEntities.ToDoTask.Find(toDoItem.ToDoId);

            toDoTask.IsCompleted = toDoItem.IsCompleted;

            dbEntities.Entry(toDoTask).State = EntityState.Modified;
            dbEntities.SaveChanges();

            //ToDo: Add exeption handling

            toDoItem.ToDoId = toDoItem.GetId();

            AddToRequestsList(toDoItem, Operation.Update);
        }

        public void DeleteToDoItem(int taskId)
        {
            /*CommunicationMessage request = requestsList.FirstOrDefault(
                msg => msg.Operation.Equals(Operation.Add) || msg.Operation.Equals(Operation.Update) && msg.ToDoItem.Equals(toDoItem));

            if (!ReferenceEquals(request, null))
            {

            }*/

            var toDoTask = dbEntities.ToDoTask.Find(taskId);

            dbEntities.ToDoTask.Remove(toDoTask);

            dbEntities.SaveChanges();

            //ToDo: Add exeption handling


            AddToRequestsList(toDoTask.ToUIEntity(), Operation.Delete);
        }

        public void ForceSync(CancellationTokenSource cancellationTokenSource)
        {
            foreach (var request in requestsList.GetConsumingEnumerable())
            {
                Task.Run(() =>
                {
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        //ToDo: Add cancellation handling
                        return;
                    }

                    SolveMethod(request);
                });
            }
            
        }

        private bool IsSyncNeeded()
        {
            return !syncToken || maxRequestCountBeforeSync <= requestsList.Count;
        }

        private void AddToRequestsList(ToDoItemViewModel toDoItem, Operation operation)
        {
            requestsList.Add(new CommunicationMessage { Operation = operation, ToDoItem = toDoItem });

            ForceSync(CancellationToken ?? new CancellationTokenSource());
        }


        private void SolveMethod(CommunicationMessage message)
        {
            switch (message.Operation)
            {
                case Operation.Add:
                    todoService.CreateItem(message.ToDoItem);
                    break;

                case Operation.Update:
                    todoService.UpdateItem(message.ToDoItem);
                    break;

                case Operation.Delete:
                    todoService.DeleteItem(message.ToDoItem.GetId());
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Dispose()
        {
            requestsList.Dispose();
        }
    }
}