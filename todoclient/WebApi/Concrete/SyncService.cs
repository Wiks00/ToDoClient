using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using WebApi.Helpers;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Concrete
{
    public class SyncService
    {
        private readonly int maxRequestCountBeforeSync;

        private readonly ToDoService todoService = new ToDoService();
        private readonly UserService userService = new UserService();

        private static List<CommunicationMessage> requestsList = new List<CommunicationMessage>();  
        private static readonly ReaderWriterLockSlim rwLockSlim =  new ReaderWriterLockSlim();

        private bool syncToken;

        public CancellationTokenSource CancellationToken { get; set; }

        public SyncService(int maxRequestCountBeforeSync = 10)
        {
            this.maxRequestCountBeforeSync = maxRequestCountBeforeSync;
            syncToken = false;
        }

        public async Task<IList<ToDoItemModel>> GetToDoItems(int userId)
        {
            List<ToDoItemModel> response =  await Task.Run(() => todoService.GetItems(userId).ToList());

            //ToDo: Db get
            //ToDo: Add exeption handling

            syncToken = true;

            return response;
        }

        public int AddToDoItem(ToDoItemModel toDoItem)
        {
            toDoItem.Name += Guid.NewGuid();

            //ToDo: Db insert
            //ToDo: Add exeption handling

            AddToRequestsList(toDoItem, Operation.Add);

            return toDoItem.ToDoId;
        }

        public void UpdateToDoItem(ToDoItemModel toDoItem)
        {
            CommunicationMessage request;

            rwLockSlim.EnterReadLock();
            try
            {
                request =
                    requestsList.FirstOrDefault(
                        msg => msg.Operation.Equals(Operation.Add) && msg.ToDoItem.Equals(toDoItem));
            }
            finally
            {
                rwLockSlim.ExitReadLock();
            }

            if (!ReferenceEquals(request, null))
            {
                rwLockSlim.EnterWriteLock();
                try
                {
                    request.ToDoItem = toDoItem;
                }
                finally
                {
                    rwLockSlim.ExitWriteLock();
                }

                return;
            }

            //ToDo: Db update
            //ToDo: Add exeption handling

            AddToRequestsList(toDoItem, Operation.Update);
        }

        public void DeleteToDoItem(ToDoItemModel toDoItem)
        {        
            CommunicationMessage request;

            rwLockSlim.EnterReadLock();
            try
            {
                request = requestsList.FirstOrDefault(msg => msg.Operation.Equals(Operation.Add) || msg.Operation.Equals(Operation.Update) && msg.ToDoItem.Equals(toDoItem));
            }
            finally
            {
                rwLockSlim.ExitReadLock();
            }

            if (!ReferenceEquals(request, null))
            {
                rwLockSlim.EnterWriteLock();
                try
                {
                    requestsList.Remove(request);
                    
                }
                finally
                {
                    rwLockSlim.ExitWriteLock();
                }

                return;
            }         

            //ToDo: Db delete
            //ToDo: Add exeption handling

            AddToRequestsList(toDoItem, Operation.Delete);
        }

        public void ForceSync(CancellationTokenSource cancellationTokenSource)
        {
            foreach (var request in requestsList)
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


            //ToDo: Test this
            requestsList.Clear();
        }

        private bool IsSyncNeeded()
        {
            return !syncToken || maxRequestCountBeforeSync <= requestsList.Count;
        }

        private void AddToRequestsList(ToDoItemModel toDoItem, Operation operation)
        {
            rwLockSlim.EnterWriteLock();
            try
            {
                if (IsSyncNeeded())
                    ForceSync(CancellationToken ?? new CancellationTokenSource());

                requestsList.Add(new CommunicationMessage { Operation = operation, ToDoItem = toDoItem });
            }
            finally
            {
                rwLockSlim.ExitWriteLock();
            }
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
    }
}