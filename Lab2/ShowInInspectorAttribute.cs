using System;

[AttributeUsage(AttributeTargets.Property)]
public class ShowInInspectorAttribute : Attribute
{
    public string Label { get; }
    public ShowInInspectorAttribute(string label = null) => Label = label;
}