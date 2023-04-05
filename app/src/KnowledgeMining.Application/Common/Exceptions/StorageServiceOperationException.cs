using System.Runtime.Serialization;

namespace KnowledgeMining.Application.Common.Exceptions
{

	[Serializable]
	public class StorageServiceOperationException : Exception
	{
		public StorageServiceOperationException() { }
		public StorageServiceOperationException(string message) : base(message) { }
		public StorageServiceOperationException(string message, Exception inner) : base(message, inner) { }
		protected StorageServiceOperationException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }
	}
}
