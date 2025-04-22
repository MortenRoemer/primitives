using System.Xml;
using MortenRoemer.ThreadSafety;

namespace MortenRoemer.Primitives.Text.Constraint;

/// <summary>
/// A predefined <see cref="IStringConstraint"/> to be used in <see cref="ConstrainedString{TConstraint}"/>.
/// This constraint accepts only valid XML-documents
/// </summary>
[ImmutableMemoryAccess(Reason = "These constraints should not use any shared resources as they are frequently used " +
                                "over multiple threads and even synchronization might add considerable overhead")]
public abstract class XmlConstraint : IStringConstraint
{
    private XmlConstraint()
    {
        // this class my never be instantiated
    }
    
    /// <inheritdoc cref="IStringConstraint.Verify(string)"/>
    public static ConstraintResult Verify(string text)
    {
        try
        {
            var document = new XmlDocument();
            document.LoadXml(text);
            return ConstraintResult.Accept;
        }
        catch (XmlException exception)
        {
            return ConstraintResult.Deny(exception.Message);
        }
    }
}