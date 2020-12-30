using System;

namespace Friflo.Json.Managed.Prop
{
	public class PropCall
	{
		public String 			msg;
		
		public virtual bool Error (String msg)
		{
			this.msg = msg;
			return false;
		}
	
		class LogCall : PropCall
		{
			public override bool Error (String msg)
			{
				// FFLog.log("PropCall.log - " + msg);
				return false;
			}		
		}
		
		public readonly static PropCall log = new LogCall();
	}
}
