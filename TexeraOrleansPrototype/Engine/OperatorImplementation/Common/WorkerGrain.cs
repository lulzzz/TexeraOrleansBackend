﻿using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;
using Orleans.Streams;
using System.Diagnostics;
using Engine.OperatorImplementation.MessagingSemantics;

namespace Engine.OperatorImplementation.Common
{
    public class Pair<T, U> 
    {
        public Pair(T first, U second) 
        {
            this.First = first;
            this.Second = second;
        }

        public T First { get; set; }
        public U Second { get; set; }
    };


    public class WorkerGrain : Grain, IWorkerGrain
    {
        protected PredicateBase predicate = null;
        protected bool isPaused = false;
        protected List<Immutable<PayloadMessage>> pausedMessages = new List<Immutable<PayloadMessage>>();
        protected Queue<TexeraTuple> outputRows = new Queue<TexeraTuple>();
        protected IAsyncStream<Immutable<TexeraMessage>> stream = null;
        protected Dictionary<Guid,Pair<int,List<IWorkerGrain>>> nextGrains = new Dictionary<Guid, Pair<int,List<IWorkerGrain>>>();
        protected IPrincipalGrain principalGrain;//for reporting status
        protected IWorkerGrain self = null;
        private IOrderingEnforcer orderingEnforcer = Utils.GetOrderingEnforcerInstance();
        private int currentEndFlagCount = 0;
        private int targetEndFlagCount = int.MinValue;
        protected virtual int BatchingLimit {get{return 1000;}}

        public virtual Task Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            this.self=self;
            this.principalGrain=principalGrain;
            this.predicate=predicate;
            return Task.CompletedTask;
        }

        public Task AddNextGrain(Guid nextOperatorGuid,IWorkerGrain grain)
        {
            if(!nextGrains.ContainsKey(nextOperatorGuid))
                nextGrains.Add(nextOperatorGuid,new Pair<int,List<IWorkerGrain>>(0, new List<IWorkerGrain>()));
            nextGrains[nextOperatorGuid].Second.Add(grain);
            return Task.CompletedTask;
        }


        public Task InitializeOutputStream(IAsyncStream<Immutable<TexeraMessage>> stream)
        {
            this.stream=stream;
            return Task.CompletedTask;
        }
        
        protected void PreProcess(Immutable<PayloadMessage> message, out List<TexeraTuple> batch,out bool isEnd)
        {
            orderingEnforcer.IndeedReceivePayloadMessage(message.Value.SenderIdentifer);
            isEnd=message.Value.IsEnd;
            batch=message.Value.Payload;
            orderingEnforcer.CheckStashed(ref batch,ref isEnd, message.Value.SenderIdentifer);  
        }

        public Task Process(Immutable<PayloadMessage> message)
        {
            List<TexeraTuple> batch;
            bool isEnd;
            PreProcess(message,out batch,out isEnd);
            List<TexeraTuple> output=ProcessBatch(batch);
            if(output!=null)
            {
                BuildPayloadMessagesThenSend(output,isEnd);
            }
            return Task.CompletedTask;
        }

        protected virtual List<TexeraTuple> ProcessBatch(List<TexeraTuple> batch)
        {
            List<TexeraTuple> result=new List<TexeraTuple>();
            foreach(TexeraTuple tuple in batch)
            {
                List<TexeraTuple> results=ProcessTuple(tuple);
                if(results!=null)
                    result.AddRange(results);
            }
            return result.Count==0?null:result;
        }

        protected virtual List<TexeraTuple> ProcessTuple(TexeraTuple tuple)
        {
            return null;
        }

        public Task AddNextGrainList(Guid nextOperatorGuid,List<IWorkerGrain> grains)
        {
            if(!nextGrains.ContainsKey(nextOperatorGuid))
                nextGrains.Add(nextOperatorGuid,new Pair<int,List<IWorkerGrain>>(0, new List<IWorkerGrain>()));
            nextGrains[nextOperatorGuid].Second.AddRange(grains);
            return Task.CompletedTask;
        }

        public Task ProcessControlMessage(Immutable<ControlMessage> message)
        {
            List<ControlMessage.ControlMessageType> executeSequence = orderingEnforcer.PreProcess(message);
            if(executeSequence!=null)
            {
                orderingEnforcer.CheckStashed(ref executeSequence,message.Value.SenderIdentifer);
                foreach(ControlMessage.ControlMessageType type in executeSequence)
                {
                    switch(type)
                    {
                        case ControlMessage.ControlMessageType.Pause:
                            Pause();
                            break;
                        case ControlMessage.ControlMessageType.Resume:
                            Resume();
                            break;
                        case ControlMessage.ControlMessageType.Start:
                            Start();
                            break;
                    }
                }
            }
            return Task.CompletedTask;
        }

        public Task ReceivePayloadMessage(Immutable<PayloadMessage> message)
        {
            if(orderingEnforcer.PreProcess(message))
            {
                if(isPaused)
                {
                    pausedMessages.Add(message);
                }
                else
                {
                    SendPayloadMessageToSelf(message, 0);
                }
            }
            return Task.CompletedTask;
        }


        private void SendPayloadMessageToSelf(Immutable<PayloadMessage> message, int retryCount)
        {
            self.Process(message).ContinueWith((t)=>
            {  
                if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                    SendPayloadMessageToSelf(message, retryCount + 1); 
            });
        }

        protected void BuildPayloadMessagesThenSend(List<TexeraTuple> outputBatch,bool isEnd)
        {
            if(isEnd)
            {
                currentEndFlagCount++;
            }
            foreach(PayloadMessage message in BuildBatchedPayloadMessage(outputBatch,currentEndFlagCount==targetEndFlagCount))
            {
                foreach(KeyValuePair<Guid, Pair<int,List<IWorkerGrain>>> entry in nextGrains)
                {
                    entry.Value.First+=1;
                    IWorkerGrain receiver=entry.Value.Second[entry.Value.First%entry.Value.Second.Count];
                    message.SequenceNumber=orderingEnforcer.GetOutMessageSequenceNumber(MakeIdentifier(receiver));
                    SendPayloadMessageTo(receiver,message.AsImmutable(),0);
                }
            }
            if(currentEndFlagCount==targetEndFlagCount)
            {
                BuildPayloadMessagesWithEndFlagThenSend();
            }
        }

        protected void BuildPayloadMessagesWithEndFlagThenSend()
        {
            PayloadMessage message = new PayloadMessage(MakeIdentifier(this),0,null,true);
            foreach(KeyValuePair<Guid, Pair<int,List<IWorkerGrain>>> entry in nextGrains)
            {
                foreach(IWorkerGrain grain in entry.Value.Second)
                {
                    message.SequenceNumber=orderingEnforcer.GetOutMessageSequenceNumber(MakeIdentifier(grain));
                    SendPayloadMessageTo(grain,message.AsImmutable(),0);
                }
            }
        }

        protected virtual void MakeFinalPayloadMessage(ref List<PayloadMessage> outputMessages)
        {
            List<TexeraTuple> payload=new List<TexeraTuple>(outputRows);
            outputMessages.Add(new PayloadMessage(MakeIdentifier(this),0,payload,false));
        }

        private void SendPayloadMessageTo(IWorkerGrain nextGrain, Immutable<PayloadMessage> message, int retryCount)
        {
            nextGrain.ReceivePayloadMessage(message).ContinueWith((t)=>
            {
                if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                    SendPayloadMessageTo(nextGrain,message, retryCount + 1);
            });
        }

        public string MakeIdentifier(IWorkerGrain grain)
        {
            string extension;
            return grain.GetPrimaryKey(out extension).ToString()+extension;
        }

        protected virtual List<PayloadMessage> BuildBatchedPayloadMessage(List<TexeraTuple> tuples,bool isEnd)
        {
            //batching limit = 1000 (for now)
            List<PayloadMessage> outputMessages=new List<PayloadMessage>();
            foreach(TexeraTuple t in tuples)
            {
                outputRows.Enqueue(t);
            }
            while(outputMessages.Count>=BatchingLimit)
            {
                List<TexeraTuple> payload=new List<TexeraTuple>();
                for(int i=0;i<BatchingLimit;++i)
                {
                    payload.Add(outputRows.Dequeue());
                }
                outputMessages.Add(new PayloadMessage(MakeIdentifier(this),0,payload,false));
            }
            if(isEnd)
            {
                MakeFinalPayloadMessage(ref outputMessages);
            }
            return outputMessages;
        }

        protected virtual void Pause()
        {
            isPaused=true;
        }

        protected virtual void Resume()
        {
            isPaused=false;
            foreach(Immutable<PayloadMessage> message in pausedMessages)
            {
                SendPayloadMessageToSelf(message,0);
            }
        }
        protected virtual void Start()
        {
            throw new NotImplementedException();
        }

        public Task SetTargetEndFlagCount(int target)
        {
            targetEndFlagCount=target;
            return Task.CompletedTask;
        }

        public Task Generate()
        {
            List<TexeraTuple> output=GenerateTuples();
            if(output!=null)
            {
                BuildPayloadMessagesThenSend(output,false);
                StartGenerate(0);
            }
            else
            {
                BuildPayloadMessagesWithEndFlagThenSend();
            }
            return Task.CompletedTask;
        }

        protected virtual List<TexeraTuple> GenerateTuples()
        {
            return null;
        }

        protected void StartGenerate(int retryCount)
        {
            self.Generate().ContinueWith((t)=>
            {
                if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                {
                    StartGenerate(retryCount);
                }
            });
        }

    }
}