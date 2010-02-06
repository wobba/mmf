using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Include this metatag in front of properties that shouldn't be serialized.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class DoNotSerializeAttribute : Attribute
{
}
