using System;
using System.Reflection;

namespace Friflo.Json.Managed.Prop
{
	public interface IPropDriver
	{
		PropField CreateVariable (PropType declType, String name, FieldInfo field);
	}
}
