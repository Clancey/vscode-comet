using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
namespace VSCodeDebug
{
	public class AggregateAsyncResult : IAsyncResult
	{
		AsyncCallback callback;
		object state;

		public AggregateAsyncResult(AsyncCallback callback, object state)
		{
			this.callback = callback;
			this.state = state;
		}

		public void CompleteAsCallback(IAsyncResult ar)
		{
			Complete();
		}

		public void Complete()
		{
			MarkCompleted();
			if (callback != null)
				callback(this);
		}

		public void CompleteWithError(Exception error)
		{
			Error = error;
			Complete();
		}

		public void CheckError(bool cancelled = false)
		{
			if (!IsCompleted)
				((IAsyncResult)this).AsyncWaitHandle.WaitOne(3000);
			if (Error != null)

				throw cancelled ? new OperationCanceledException() : Error;
		}

		void MarkCompleted()
		{
			lock (this) {
				IsCompleted = true;
				if (waitHandle != null)
					waitHandle.Set();
			}
		}

		public Exception Error { get; private set; }
		public bool IsCompleted { get; private set; }

		object IAsyncResult.AsyncState { get { return state; } }

		ManualResetEvent waitHandle;

		WaitHandle IAsyncResult.AsyncWaitHandle {
			get {
				lock (this) {
					if (waitHandle == null)
						waitHandle = new ManualResetEvent(IsCompleted);
				}
				return waitHandle;
			}
		}

		bool IAsyncResult.CompletedSynchronously {
			get { return false; }
		}
	}

	public abstract class CancellableAsyncCommand : AggregateAsyncResult
	{
		bool cancelled;
		IAsyncResult innerResult;

		public CancellableAsyncCommand(AsyncCallback callback, object state) :
			base(callback, state)
		{

		}

		public bool CheckCancelled()
		{
			if (cancelled) {
				CompleteWithError(new OperationCanceledException());
			}
			return false;
		}

		public void SetInnerResult(IAsyncResult result)
		{
			lock (this) {
				if (!cancelled) {
					innerResult = result;
					return;
				}
			}
			CancelInnerResult(result);
		}

		protected abstract void CancelInnerResult(IAsyncResult innerResult);

		public void Cancel()
		{
			IAsyncResult toCancel;
			lock (this) {
				if (cancelled)
					return;
				cancelled = true;
				toCancel = innerResult;
				innerResult = null;
			}
			if (toCancel != null) {
				CancelInnerResult(toCancel);
			}
		}
	}
}