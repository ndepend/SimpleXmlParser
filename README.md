# Simple Xml Parser (C# and Java)

## Why Simple Xml Parser

Recently (July 2022) we faced one of the most dreaded situation for an ISV: several users reported simultaneously that some code gets broken. The concerned code was working fine and left untouched for 8 years!

The code is the <a href="https://www.ndepend.com/docs/teamcity-integration-ndepend" target="_blank" rel="noopener">NDepend TeamCity plugin</a>. The break occurs when <a href="https://youtrack.jetbrains.com/issue/TW-76866" target="_blank" rel="noopener">upgrading to Team City 2022.04.2 (build 108655) or higher</a>. This is because the Java version used by this TC version is 11. The JAXB API has been removed in Java 11 to reduce the framework footprint as explained in <a href="https://openjdk.org/jeps/320" target="_blank" rel="noopener">JEP 320 proposal</a> and our TeamCity plugin needs to parse some XML. If you are concerned by this bug just <a href="https://www.ndepend.com/download" target="_blank" rel="noopener">download the latest NDepend version</a> and re-install the TC plugin.

To solve this issue we had the choice to:
<ul>
 	<li><a href="https://stackoverflow.com/a/43574427/27194" target="_blank" rel="noopener">Embed our own copy of the Java EE APIs on the classpath or module path</a>.</li>
 	<li>Parse the XML ourselves. This is the option we choose because the one above could lead to all sort of collisions depending on the TeamCity and Java versions installed.</li>
</ul>



## Simple Xml Parser Usage and Capabilities

Being much more proficient with C# than with Java, it made sense to first write the code and tests in C# and then convert it to Java. The code mostly use <code>string</code>, <code>char</code>, <code>StringBuilder</code> and <code>List&lt;T&gt;</code> that are quite similar in both platforms. Here is the <a href="https://github.com/ndepend/SimpleXmlParser/blob/master/SimpleXmlParser/XmlParsing.cs" target="_blank" rel="noopener">C# version</a> and the <a href="https://github.com/ndepend/SimpleXmlParser/blob/master/JavaCode/XmlParsing.java" target="_blank" rel="noopener">Java version</a>.

The idea is to produce our custom DOM: a hierarchy of custom <a href="https://github.com/ndepend/SimpleXmlParser/blob/master/SimpleXmlParser/XmlParsing.cs#L33" target="_blank" rel="noopener"><code>XmlElement</code></a> and <a href="https://github.com/ndepend/SimpleXmlParser/blob/master/SimpleXmlParser/XmlParsing.cs#L26" target="_blank" rel="noopener"><code>XmlAttribute</code></a> objects. This hierarchy can then be consumed into your own code to populate your model, like we do with <a href="https://github.com/ndepend/SimpleXmlParser/blob/master/SimpleXmlParser/InspectionsModel/FillInspectionsModel.cs" target="_blank" rel="noopener"><code>FillInspectionModel</code></a>.

This XML parser supports:
<ul>
 	<li><code>&lt;Foo&gt;...&lt;/Foo&gt;</code> tags</li>
 	<li><code>&lt;Foo /&gt;</code> tags</li>
 	<li><code>&lt;Foo Attr="Value" /&gt;</code> attributes</li>
 	<li><code>&lt;Foo&gt;&amp;lt; &amp;gt; &amp;amp; &amp;quot; &amp;#13; &amp;#10;&lt;/Foo&gt;</code> special characters which translate to <code>&lt; &gt; &amp; " \r \n</code></li>
 	<li><code>&lt;![CDATA[raw content]]&gt;</code> sections</li>
 	<li><code>&lt;!--xyz--&gt;</code> comments</li>
</ul>
We believe that for our usage it is bug free because it is fully tested for all XML documents our users provided us with. It is also <a href="https://github.com/ndepend/SimpleXmlParser/tree/master/SimpleXmlParserTests" target="_blank" rel="noopener">100% covered by the test suite</a>. <strong>However the purpose was not to support the entire XML specification so it is certainly buggy for more advanced usages</strong>.

![Simple Xml Parser Coverage](https://user-images.githubusercontent.com/511445/182353472-a2ab5125-6df1-47c8-90a7-9bc25f41b40d.png)


## Simple Xml Parser Design

Our plan is to only fix potential bugs users might face in the context of the NDepend TeamCity plugin but not to improve the overall XML support. Hence the design and performance were not a priority and they could be improved in numerous ways.

Here is the overall Architecture:

<ul>
  <li>The parser does two passes. The first pass produces some <code>XmlRow</code> objects and the second pass fill the <code>XmlElement</code> and <code>XmlAttribute</code> model from the rows.</li>
  <li>Then <code>FillInspectionModel</code> fills our own model from the DOM.</li>
</ul>


![Simple Xml Parser Graph](https://user-images.githubusercontent.com/511445/182350345-e08a1315-817d-4b26-967b-ee3c8fe6b904.png)



