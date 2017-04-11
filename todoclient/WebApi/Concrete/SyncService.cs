﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Concrete
{
    public class SyncService
    {
        private readonly bool syncToken;
        private readonly int maxRequestCountBeforeSync;
        private int syncCounter;

        private readonly ToDoService todoService = new ToDoService();
        private readonly UserService userService = new UserService();

        private ConcurrentQueue<CommunicationMessage> concurrentQueue;

        public SyncService(int maxRequestCountBeforeSync = 10)
        {
            this.maxRequestCountBeforeSync = maxRequestCountBeforeSync;
            syncCounter = 0;
            syncToken = false;
            concurrentQueue = new ConcurrentQueue<CommunicationMessage>();
        }

        public int AddToDoItem(ToDoItemModel toDoItem)
        {
            //ToDo: Db insert

            concurrentQueue.Enqueue(new CommunicationMessage { Operation = Operation.Add, ToDoItem = toDoItem});

            return toDoItem.ToDoId;
        }


        public void SyncAsync(CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                Parallel.ForEach(concurrentQueue, new ParallelOptions {CancellationToken = cancellationTokenSource.Token},
                SolveMethod);
            }
            catch (OperationCanceledException ex)
            {
                //ToDo: Add exeption handling
            }
            finally
            {
                cancellationTokenSource.Dispose();
                syncCounter = 0;
            }
        }

        private bool IsSyncNeeded()
        {
            return syncToken || maxRequestCountBeforeSync <= syncCounter;
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
                    todoService.DeleteItem(int.Parse(message.ToDoItem.Name.Substring(message.ToDoItem.Name.LastIndexOf(",", StringComparison.InvariantCultureIgnoreCase) + 1)));
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}