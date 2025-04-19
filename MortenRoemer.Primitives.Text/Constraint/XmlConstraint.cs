using System.Xml;

namespace MortenRoemer.Primitives.Text.Constraint;

/// <summary>
/// A predefined <see cref="IStringConstraint"/> to be used in <see cref="ConstrainedString{TConstraint}"/>.
/// This constraint accepts only valid XML-documents
/// </summary>
public abstract class XmlConstraint : IStringConstraint
{
    private const string Message = "XML files must conform to the specification of the XML format";

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