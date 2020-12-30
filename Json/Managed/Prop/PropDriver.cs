using System;
using System.Reflection;

namespace Friflo.Json.Managed.Prop
{
	public class PropDriver
	{
						static	IPropDriver	platform  = null;
	
		private class DefaultDriver : IPropDriver
		{
			public PropField CreateVariable(PropType declType, String name, FieldInfo field)
			{
				return new PropFieldVariable(declType, name, field);
			}		
		}	
	
		public static void SetDriver(IPropDriver driver)
		{
			platform = driver;
		}
	
		public static IPropDriver GetDriver()
		{
			if (platform != null)
				return platform;








			platform = new DefaultDriver();

			return platform;
		}
	}
}
