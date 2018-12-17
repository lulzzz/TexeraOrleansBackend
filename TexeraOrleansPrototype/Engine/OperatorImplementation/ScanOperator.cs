using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Orleans.Concurrency;
using Engine.OperatorImplementation.Interfaces;
using TexeraUtilities;

namespace Engine.OperatorImplementation
{
    public class ScanOperator : Grain, IScanOperator
    {
        public List<TexeraTuple> Rows = new List<TexeraTuple>();
        public INormalGrain nextOperator;
        System.IO.StreamReader file;

        public override Task OnActivateAsync()
        {
            
            nextOperator = this.GrainFactory.GetGrain<IFilterOperator>(this.GetPrimaryKeyLong(), Constants.AssemblyPath);

            string p2;
            if (Constants.num_scan == 1)
                p2 = Constants.dir + Constants.dataset + "_input.csv";
            else
                p2 = Constants.dir + Constants.dataset + "_input" + "_" + (this.GetPrimaryKeyLong() - 1) + ".csv";
            file = new System.IO.StreamReader(p2);
            return base.OnActivateAsync();
        }
        public override Task OnDeactivateAsync()
        {
            return base.OnDeactivateAsync();
        }

        public async Task PauseGrain()
        {
            await nextOperator.PauseGrain();
        }

        public async Task ResumeGrain()
        {
            await nextOperator.ResumeGrain();
            nextOperator.StartProcessAfterPause();
        }

        public async Task SubmitTuples() 
        {
            List<TexeraTuple> batch = new List<TexeraTuple>();
            ulong seq = 0;

            for (int i = 1; i <= Rows.Count; ++i)
            {
                batch.Add(Rows[i-1]);
                if(i%Constants.batchSize == 0)
                {
                    batch[0].seq_token = seq++;
                    // TODO: We can't call batch.Clear() after this because it somehow ends
                    // up clearing the memory and the next grain gets list with no tuples.
                    nextOperator.Process(batch.AsImmutable());
                    // batch.Clear();
                    batch = new List<TexeraTuple>();
                }
	        }

            // Console.WriteLine(seq);
            if(batch.Count > 0)
            {
                batch[0].seq_token = seq++;
                nextOperator.Process(batch.AsImmutable());
                // batch.Clear();
                batch = new List<TexeraTuple>();
            }

            // Console.WriteLine("Seq num for last tuple " + seq);
            batch.Add(new TexeraTuple(seq ,- 1, null));
            nextOperator.Process(batch.AsImmutable());
            Console.WriteLine("Scan " + (this.GetPrimaryKeyLong()).ToString() + " sending done");
           // return Task.CompletedTask;
        }


        public Task LoadTuples()
        {
            string line;
            ulong count = 0;
            while ((line = file.ReadLine()) != null)
            {
                // The sequence token filled here will be replaced later in SubmitTuples().
                Rows.Add(new TexeraTuple(count, (int)count, line.Split(",")));
                count++;
            }
            Console.WriteLine("Scan " + (this.GetPrimaryKeyLong()).ToString() + " loading done");
            return Task.CompletedTask;
        }
       
    }
}