using System;

namespace Friflo.Json.Managed.Utils
{
	// FrifloIOException
	public class FrifloException : Exception
	{
		public FrifloException()
		:
			base("FrifloException") {
		}

		public FrifloException(String message)
		:
			base (message) {
		}

		public FrifloException(String message, Exception cause)
		:
			base (message, cause) {
		}





	}
}
