

using System.Text;

class Element
{
    //elemnt structure and child element
    public string elementName = "";
    public string InsideElement = "";
    public Dictionary<string, string> attributes = new Dictionary<string, string>();

    public List<Element> ChildElements = new List<Element>();

    //--constructors--//
    public Element(string elementName, string insideElement)
    {
        this.elementName = elementName;
        this.InsideElement = insideElement;
    }
    public Element(Element ele)
    {
        this.elementName = ele.elementName;
        this.InsideElement = ele.InsideElement;
        this.attributes = ele.attributes;
        this.ChildElements = ele.ChildElements;
    }
    public Element() { }


    //--create and delete child--//
    //add child to current element
    public void createChild(string elementName, string insideElement)
    {
        ChildElements.Add(new Element(elementName, insideElement));
    }
    //deletes first occurence
    public void deleteChild(string childName)
    {
        for (int i = 0; i < ChildElements.Count; i++)
        {
            if (ChildElements[i].elementName == childName)
            {
                ChildElements.RemoveAt(i);
                return;
            }
        }
    }

    public List<Element> getChildren(string childName)
    {
        List<Element> children = new List<Element>();
        foreach (Element child in this.ChildElements)
        {
            if (child.elementName == childName)
            {
                children.Add(child);
            }
        }
        return children;
    }

    //--Attribute CURD(create update read delete)--//
    //add attribute to current element
    public void addAttribute(string attributeName, string value)
    {
        if (this.attributes.ContainsKey(attributeName)) { return; }
        this.attributes.Add(attributeName, value);
    }
    //delete attribute
    public void deleteAttribute(string attributeName)
    {
        this.attributes.Remove(attributeName);
    }
    //modify attribute (completely rewrites)
    public void modifyAttributeValue(string attributeName, string value)
    {
        if (!this.attributes.ContainsKey(attributeName))
        {
            addAttribute(attributeName, value);
        }
        else { this.attributes[attributeName] = value; }
    }
    //get attrubute value, if it doesnt exist return null
    public string getAttributeValue(string attributeName)
    {
        if (!this.attributes.ContainsKey(attributeName)) { return "NULL"; }
        return this.attributes[attributeName];
    }


    //--consructDoc and save--//
    //construct elements into one string
    public string constructDoc(int num = 0, bool debug = false)
    {
        string doc = "";

        doc += indentCreator(num, debug) + "<" + this.elementName;
        string[] keyArray = new string[this.attributes.Keys.Count];
        this.attributes.Keys.CopyTo(keyArray, 0);
        for (int i = 0; i < keyArray.Length; ++i)
        {
            doc += " " + keyArray[i] + "=\"" + this.attributes[keyArray[i]] + "\"";
        }
        doc += ">" + endLine(debug);

        if (this.InsideElement != "" && this.InsideElement != string.Empty)
        {
            doc += indentCreator(num + 1, debug) + this.InsideElement + endLine(debug);
        }

        for (int i = 0; i < ChildElements.Count; ++i)
        {
            doc += ChildElements[i].constructDoc(num + 1, debug);
        }

        doc += indentCreator(num, debug) + "</" + elementName + ">" + endLine(debug);
        return doc;
    }
    //save elements to file
    public void SaveToFile(string nameName, string filepath = "./")
    {
        byte[] fileWriter = Encoding.UTF8.GetBytes(constructDoc(debug: true));
        FileStream OpenedFile = File.Open(filepath + nameName, FileMode.Create);
        OpenedFile.Write(fileWriter);
        OpenedFile.Close();
    }


    //--parseing input--//
    //delimit with "<"
    void parseMarkup(List<string> dataSplit)
    {
        if (dataSplit.Count == 0) { return; }
        string operatingDataLine = dataSplit[0];
        string[] nameAndAttributes = new string[0];

        if (operatingDataLine.Split(">", StringSplitOptions.RemoveEmptyEntries).Length > 1)
        {
            this.InsideElement = operatingDataLine.Split(">", StringSplitOptions.RemoveEmptyEntries)[1];
        }
        operatingDataLine = operatingDataLine.Split(">", StringSplitOptions.RemoveEmptyEntries)[0];

        dataSplit.RemoveAt(0);

        //split data at " "(spaces)
        {
            bool inQuotes = false;
            string tempstring = "";
            List<string> stringList = new List<string>();
            for (int i = 0; i < operatingDataLine.Length; i++)
            {
                char letter = operatingDataLine[i];
                if (letter == '"') { inQuotes = !inQuotes; }
                if (i == operatingDataLine.Length - 1) { tempstring += letter; stringList.Add(tempstring); }
                if (letter == ' ' && !inQuotes) { stringList.Add(tempstring); tempstring = ""; }
                else
                {
                    tempstring += letter;
                }
            }
            nameAndAttributes = stringList.ToArray();
        }

        //sets current element name and atributes
        for (int i = 0; i < nameAndAttributes.Length; i++)
        {
            if (i == 0) { this.elementName = nameAndAttributes[i]; }
            else
            {
                string[] nameValue = nameAndAttributes[i].Split("=", 2);
                if (nameValue.Length <= 1)
                { addAttribute(nameValue[0], "NULL"); }
                else { addAttribute(nameValue[0], nameValue[1].Trim('"')); }
            }
        }

        //loops till either count is 0 or the end tag is reached
        while (dataSplit.Count > 0)
        {
            if (dataSplit[0] == "/" + this.elementName + ">")
            {
                dataSplit.RemoveAt(0);
                break;
            }
            else
            {
                this.ChildElements.Add(new Element());
                this.ChildElements[^1].parseMarkup(dataSplit);
            }
        }
    }
    //parse from string
    public void parseString(string data)
    {
        parseMarkup(Element.convertArrayToList(data.Split("<", StringSplitOptions.RemoveEmptyEntries)));
    }
    //parse from file
    //this needs "./"
    public void parseFile(string fileName)
    {
        string singleData = "";
        {
            string[] data = File.ReadAllLines(fileName);
            foreach (string var in data)
            {
                singleData += var.Trim(new char[] { '\t', ' ', (char)0x09 });
            }
        }
        parseMarkup(Element.convertArrayToList(singleData.Split("<", StringSplitOptions.RemoveEmptyEntries)));
    }


    public string convertMarkupToJson(int index = 0, bool debug = false)
    {
        string conjoinedChildrenVals = "";
        List<string> headNumberOfOccu = new List<string>();

        //if there are no children
        if (this.ChildElements.Count == 0)
        {
            return "\"" + this.InsideElement + "\"";
        }

        for (int i = 0; i < this.ChildElements.Count; i++)
        {
            if (!headNumberOfOccu.Contains(this.ChildElements[i].elementName))
            {
                headNumberOfOccu.Add(this.ChildElements[i].elementName);
            }
        }

        conjoinedChildrenVals = "{" + endLine(debug);
        //if there are children
        for (int i = 0; i < headNumberOfOccu.Count; i++)
        {
            List<Element> children = getChildren(headNumberOfOccu[i]);
            //gets all children values
            if (children.Count == 1)
            {
                conjoinedChildrenVals += indentCreator(index, debug) + "\"" + children[0].elementName + "\"" + ":" + children[0].convertMarkupToJson(index + 1, debug);
            }
            else
            {
                conjoinedChildrenVals += indentCreator(index, debug) + "\"" + headNumberOfOccu[i] + "\":[" + endLine(debug);
                for (int j = 0; j < children.Count; j++)
                {
                    conjoinedChildrenVals += indentCreator(index, debug) + children[j].convertMarkupToJson(index + 1, debug) + (j == children.Count - 1 ? "" : ",") + endLine(debug);
                }
                conjoinedChildrenVals += indentCreator(index, debug) + "]";
            }
            conjoinedChildrenVals += (i == headNumberOfOccu.Count - 1 ? "" : ",") + endLine(debug);
        }
        conjoinedChildrenVals += "}" + endLine(debug);

        return (index == 0 ? "{" + endLine(debug) + indentCreator(index, debug) + "\"" + this.elementName + "\":" : "") + conjoinedChildrenVals + (index == 0 ? "}" : "");
    }


    //--quality of life--//
    string indentCreator(int num, bool debug)
    {
        if (!debug) { return ""; }
        string indents = "";
        for (int i = 0; i < num; ++i)
        {
            indents += "\t";
        }
        return indents;
    }
    string endLine(bool debug)
    {
        return debug ? "\n" : "";
    }

    public static List<string> convertArrayToList(string[] array)
    {
        List<string> list = new List<string>();
        foreach (string var in array) { list.Add(var); }
        return list;
    }
    public static string[] MergeArrayIndex(int start, int last, string[] array, string joiningChar = "")
    {
        List<string> newArray = new List<string>();
        bool boolstart = false;
        for (int i = 0; i < array.Length; i++)
        {
            if (i >= start && i <= last)
            {
                if (!boolstart) { newArray.Add(array[i]); boolstart = true; }
                else
                {
                    newArray[^1] += joiningChar + array[i];
                }
            }
            else
            {
                newArray.Add(array[i]);
            }
        }
        //dg
        return newArray.ToArray();
    }

    //static int Main()
    //{
    //    Element test = new Element("root", "");
    //    test.createChild("tab1", "test");
    //    test.createChild("tab2", "test1");
    //    test.createChild("tab2", "test3");

    //    Console.WriteLine(test.convertMarkupToJson(debug: true));


    //    return 0;
    //}

}

