using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// supprimer les methodes sans operationid
void SupprimerPathsSansOperationId(JObject root)
{
    var paths = (JObject)root["paths"];
    var pathsToRemove = new List<string>();
    foreach (var path in paths.Properties())
    {
        var methods = (JObject)path.Value;
        var toRemove = new List<string>();
        foreach (var method in methods.Properties())
        {
            if (method.Value["operationId"] == null)
                toRemove.Add(method.Name);
        }
        foreach (var methodName in toRemove)
            methods.Remove(methodName);
        if (!methods.HasValues)
            pathsToRemove.Add(path.Name);
    }
    foreach (var pathName in pathsToRemove)
        paths.Remove(pathName);
}

// supprimer parameters x-userid
// supprimer param userid de get user
// supprimer lignes allowEmptyValue:
// retiré - ajoute x-ms-url-encoding: single
// ajoute x-ms-summary à partir de la description
void ModifierMethodes(JObject root)
{
    var paths = (JObject)root["paths"];
    foreach (var path in paths.Properties())
    {
        string originalPath = path.Name;
        var methods = (JObject)path.Value;
        foreach (var method in methods.Properties())
        {
            var methodObject = (JObject)method.Value;
            if (methodObject.TryGetValue("parameters", out JToken? parametersToken) && parametersToken is JArray parameters)
            {
                var toRemove = new List<JToken>();
                foreach (var param in parameters)
                {
                    if (param["name"]?.ToString() == "X-UserId")
                        toRemove.Add(param);
                    if (originalPath == "/api/v1.0/organizations/user-infos/{userId}" && param["name"]?.ToString() == "userId")
                        toRemove.Add(param);
                    if (param is JObject paramObj && paramObj.ContainsKey("allowEmptyValue"))
                        paramObj.Remove("allowEmptyValue");
                    //if (param is JObject paramObj2 && paramObj2.ContainsKey("in") && paramObj2["in"]?.ToString() == "header")
                    //    paramObj2["x-ms-url-encoding"] = "single";
                    if (param is JObject paramObj3 && paramObj3.ContainsKey("in"))
                    {
                        if (paramObj3["description"]?.ToString() == "")
                            paramObj3["description"] = paramObj3["name"];
                        paramObj3["x-ms-summary"] = paramObj3["description"];
                    }
                }
                foreach (var item in toRemove)
                    parameters.Remove(item);
            }
        }
    }
}

// retirer json-patch+json et text/json dans les consumes
void ModifierConsumes (JObject root)
{
    var paths = (JObject)root["paths"];
    foreach (var path in paths.Properties())
    {
        var methods = (JObject)path.Value;
        foreach (var method in methods.Properties())
        {
            var methodObject = (JObject)method.Value;
            if (methodObject.ContainsKey("consumes"))
            {
                var consumes = (JArray)methodObject["consumes"];
                var itemToRemove = consumes.FirstOrDefault(c => c.ToString() == "text/json");
                if (itemToRemove != null)
                    consumes.Remove(itemToRemove);
                var itemToRemove2 = consumes.FirstOrDefault(c => c.ToString() == "application/json-patch+json");
                if (itemToRemove2 != null)
                    consumes.Remove(itemToRemove2);
            }
        }
    }
}

// retirer /api/v1.0 des paths
// Remplacer GetUser par GetCurrentUser
void ModifierEndpointPath(JObject root)
{
    var paths = (JObject)root["paths"];
    JObject newPaths = new JObject();
    foreach (var path in paths.Properties())
    {
        string originalPath = path.Name;
        string newPath = originalPath.Replace("/api/v1.0", "");
        var methods = (JObject)path.Value;
        if (newPath == "/organizations/user-infos/{userId}")
        {
            newPath = "/organizations/user-infos/me";
            foreach (var methodProp in methods.Properties())
            {
                var method = (JObject)methodProp.Value;
                if (method.ContainsKey("summary"))
                    method["summary"] = "Organization - Get connected user";
                if (method.ContainsKey("description"))
                    method["description"] = "Get your user informations";
            }
        }
        newPaths[newPath] = methods;
    }
    root["paths"]=newPaths;
}

// ajouter init et InitParameters
void AjouterInitPath(JObject root,string jsonOrigTemp)
{
    var paths = (JObject)root["paths"];
    string jsonFileTemp = File.ReadAllText(jsonOrigTemp);
    JObject rootTemp = JObject.Parse(jsonFileTemp);
    var pathsTemp = (JObject)rootTemp["paths"];
    foreach (var path in pathsTemp.Properties())
    {
        var methods = (JObject)path.Value;
        string newPath = path.Name;
        paths.Add(newPath, methods);
    }
}

// ajouter triggers 
void AjouterTriggers(JObject root, string jsonOrigTemp)
{
    var paths = (JObject)root["paths"];
    var jsonFileTemp = File.ReadAllText(jsonOrigTemp);
    var rootTemp = JObject.Parse(jsonFileTemp);
    var pathsTemp = (JObject)rootTemp["paths"];
    foreach (var path in pathsTemp.Properties())
    {
        var methods = (JObject)path.Value;
        string oldPath = path.Name;

        if (oldPath != "/webhooks/ev/201")
        {
            paths.Add(oldPath, methods);
            continue;
        }

        var newPath = oldPath.Replace("/201", "/201");
        foreach (var methodProp in methods.Properties())
        {
            var method = (JObject)methodProp.Value;
            method["operationId"] = "ThenObsCreated";
            method["summary"] = "Trigger - Observation created";
            method["description"] = "Trigger the flow when an observation is created";
        }
        paths.Add(newPath, methods);

        newPath = oldPath.Replace("/201", "/202");
        foreach (var methodProp in methods.Properties())
        {
            var method = (JObject)methodProp.Value;
            method["operationId"] = "ThenObsUpdated";
            method["summary"] = "Trigger - Observation updated";
            method["description"] = "Trigger the flow when an observation is updated";
        }
        paths.Add(newPath, methods);

        newPath = oldPath.Replace("/201", "/301");
        foreach (var methodProp in methods.Properties())
        {
            var method = (JObject)methodProp.Value;
            method["operationId"] = "ThenFormCreated";
            method["summary"] = "Trigger - Form created";
            method["description"] = "Trigger the flow when a form is created";
        }
        paths.Add(newPath, methods);

        newPath = oldPath.Replace("/201", "/302");
        foreach (var methodProp in methods.Properties())
        {
            var method = (JObject)methodProp.Value;
            method["operationId"] = "ThenFormUpdated";
            method["summary"] = "Trigger - Form updated";
            method["description"] = "Trigger the flow when a form is updated";
        }
        paths.Add(newPath, methods);

        newPath = oldPath.Replace("/201", "/401");
        foreach (var methodProp in methods.Properties())
        {
            var method = (JObject)methodProp.Value;
            method["operationId"] = "ThenDocumentCreated";
            method["summary"] = "Trigger - Document created";
            method["description"] = "Trigger the flow when a document is created";
        }
        paths.Add(newPath, methods);
    }
}

void SupprimerPathProperties(JObject root)
{
    var paths = (JObject)root["paths"];
    foreach (var path in paths.Properties())
    {
        var methods = (JObject)path.Value;
        foreach (var method in methods.Properties())
        {
            var methodObject = (JObject)method.Value;
            var parameters = (JArray)methodObject["parameters"];
            if (parameters == null)
                continue;
            var toRemove = new List<JToken>();
            foreach (var param in parameters) {
                if (param is JObject paramObj)
                {
                    if (paramObj.ContainsKey("format"))
                    {
                        string formatValue = param["format"].ToString();
                        if (formatValue == "date-span" || formatValue == "date-time" || formatValue == "uuid")
                            toRemove.Add(param);
                        if (paramObj.ContainsKey("type"))
                        {
                            string typeValue = param["type"].ToString();
                            if (typeValue== "integer")
                                toRemove.Add(param);
                        }
                    }
                }
            }
            foreach (var item in toRemove)
                parameters.Remove(item);
        }
    }

}

// supprimer les classes en erreur !! liste en dur
void SupprimerClassesEnErreur(JObject root)
{
    var definitions = (JObject)root["definitions"];
    var defNameToRemove = new List<string>() { "FormField" , "ForminstanceAnswer" , "FormPart","FormSection", "FormTemplateInstance", "GetModulesOrga",
                                            "GridInstance", "ModuleContent", "ModuleOrga", "OrgaForm" };
    foreach (var name in defNameToRemove)
        definitions.Remove(name);
}

// supprimer properties de dfinitions
// format uuid , date-time , number
void SupprimerClassesProperties(JObject root)
{
    var definitions = (JObject)root["definitions"];
    foreach (var definitionProp in definitions.Properties())
    {
        var properties = (JObject)definitionProp.Value["properties"];
        if (properties == null)
            continue;
        foreach (var prop in properties.Properties())
        {
            var property = (JObject)prop.Value;
            if (property.ContainsKey("items"))
            {
                var items = (JObject)property["items"];
                if (items.ContainsKey("format"))
                {
                    string formatValue = items["format"].ToString();
                    if (formatValue == "date-span" || formatValue == "date-time" || formatValue == "uuid")
                        items.Remove("format");
                }
                if (items.ContainsKey("type") && items["type"].ToString() == "integer")
                {
                    if (items.ContainsKey("format"))
                        items.Remove("format");
                }
            }
            if (property.ContainsKey("format"))
            {
                string formatValue = property["format"].ToString();
                if (formatValue == "date-span" || formatValue == "date-time" || formatValue == "uuid")
                    property.Remove("format");
                if (property.ContainsKey("type") && property["type"].ToString() == "integer")
                    property.Remove("format");
            }
        }
    }

}

// remplacer blocssecurity securitydefinitions
void RemplacerBlocsSecu(JObject root, string jsonSecuTemp)
{
    string jsonFileTemp = File.ReadAllText(jsonSecuTemp);
    JObject rootTemp = JObject.Parse(jsonFileTemp);
    root.Remove("securityDefinitions");
    root.Remove("security");
    root.Add("securityDefinitions", rootTemp["securityDefinitions"]);
    root.Add("security", rootTemp["security"]);
}

// Remplacer des blocs d'info et hosts et ...
void RemplacerBlocsInfos(JObject root, string jsonInfoTemp)
{
    string jsonFileTemp = File.ReadAllText(jsonInfoTemp);
    JObject rootTemp = JObject.Parse(jsonFileTemp);
    root.Remove("info");    root.Add("info", rootTemp["info"]);
    root.Remove("host"); root.Add("host", rootTemp["host"]);
    root.Remove("basePath"); root.Add("basePath", rootTemp["basePath"]);
    root.Remove("x-ms-connector-metadata"); root.Add("x-ms-connector-metadata", rootTemp["x-ms-connector-metadata"]);
    root.Remove("x-authentication"); root.Add("x-authentication", rootTemp["x-authentication"]);
}

// modif reponse GetOrganizations
void RemplacerResponceGetOrga(JObject root, string jsonRespOrgaTemp)
{
    string jsonFileTemp = File.ReadAllText(jsonRespOrgaTemp);
    JObject rootTemp = JObject.Parse(jsonFileTemp);
    var paths = (JObject)root["paths"];
    var pathCible = root["paths"]?["/organizations"]["get"];
    var pathSourc = rootTemp["paths"]?["/organizations"]["get"];
    pathCible["responses"] = pathSourc["responses"].DeepClone();
}

// modif parametres chunkappend
void RemplacerParamsChunk(JObject root, string jsonParamChunkTemp)
{
    string jsonFileTemp = File.ReadAllText(jsonParamChunkTemp);
    JObject rootTemp = JObject.Parse(jsonFileTemp);
    var paths = (JObject)root["paths"];
    var pathCible = root["paths"]?["/medias/{mediaId}/uploadappend"]["post"];
    var pathSourc = rootTemp["paths"]?["/medias/{mediaId}/uploadappend"]["post"];
    pathCible["consumes"] = pathSourc["consumes"].DeepClone();
    pathCible["parameters"] = pathSourc["parameters"].DeepClone();
}

//------------------------------------------------------------------------------------------------------------------------------
void Controle(JObject root)
{
    Console.WriteLine("----------- Contrôle ---------------------------------");
    var paths = (JObject)root["paths"];
    foreach (var path in paths.Properties())
    {
        string originalPath = path.Name;
        var methods = (JObject)path.Value;
        foreach (var method in methods.Properties())
        {
            var methodObject = (JObject)method.Value;
            var methodId = method.Value["operationId"];
            if (methodObject.TryGetValue("parameters", out JToken? parametersToken) && parametersToken is JArray parameters)
            {
                foreach (var param in parameters)
                {
                    var paramName = param["name"]?.ToString();
                    if (param["description"]?.ToString() == "" || param["x-ms-summary"]?.ToString() == "")
                        Console.WriteLine($"Warning : {methodId} - {paramName} - description ou x-ms-summary vides");
                    if (param["description"] == null)
                        Console.WriteLine($"Warning : {methodId} - {paramName} - pas de description");
                }
            }
        }
    }
}

//------------------------------------------------------------------------------------------------------------------------------

#region comparaison
void Compare(JObject root, string oldVersionFile)
{
    Console.WriteLine("----------- Compare ancienne version ---------------------------------");
    string jsonOld = File.ReadAllText(oldVersionFile);
    JObject rootOld = JObject.Parse(jsonOld);

    var pathsNew = root["paths"]?.ToObject<Dictionary<string, JObject>>() ?? new();
    var pathsOld = rootOld["paths"]?.ToObject<Dictionary<string, JObject>>() ?? new();
    var allPaths = new HashSet<string>(pathsNew.Keys.Concat(pathsOld.Keys));

    foreach (var path in allPaths.OrderBy(p => p))
    {
        var in1 = pathsNew.ContainsKey(path);
        var in2 = pathsOld.ContainsKey(path);

        if (!in1)
        {
            Console.WriteLine($"Path uniquement dans old : {path}");
            continue;
        }
        if (!in2)
        {
            Console.WriteLine($"Path uniquement dans new : {path}");
            continue;
        }
        var methods1 = pathsNew[path].Properties().Select(p => p.Name.ToLowerInvariant()).ToHashSet();
        var methods2 = pathsOld[path].Properties().Select(p => p.Name.ToLowerInvariant()).ToHashSet();
        var allMethods = new HashSet<string>(methods1.Concat(methods2));
        foreach (var method in allMethods)
        {
            var has1 = methods1.Contains(method);
            var has2 = methods2.Contains(method);

            if (!has1)
                Console.WriteLine($"{path} Méthode '{method}' uniquement dans old");
            else if (!has2)
                Console.WriteLine($"{path} Méthode '{method}' uniquement dans new");
            else
            {
                var methodObjNew = pathsNew[path][method];
                var methodObjOld = pathsOld[path][method];

                if (!JToken.DeepEquals(methodObjNew, methodObjOld))
                {
                    Console.WriteLine($"{path} Méthode '{method}' différente entre les deux");
                    var sortedOld = NormalizeJToken(methodObjOld);
                    var sortedNew = NormalizeJToken(methodObjNew);
                    DiffJTokens(sortedOld, sortedNew, $"paths/{path}/{method}");
                }
            }
        }
    }
}

static void DiffJTokens(JToken token1, JToken token2, string path = "")
{
    if (JToken.DeepEquals(token1, token2))
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"= {path}: {token1?.ToString(Formatting.None)}");
        Console.ResetColor();
        return;
    }

    if (token1 == null)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"+ {path}: {token2?.ToString(Formatting.None)}");
        Console.ResetColor();
        return;
    }

    if (token2 == null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"- {path}: {token1?.ToString(Formatting.None)}");
        Console.ResetColor();
        return;
    }

    if (token1.Type != token2.Type)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"- {path}: {token1?.ToString(Formatting.None)}");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"+ {path}: {token2?.ToString(Formatting.None)}");
        Console.ResetColor();
        return;
    }

    if (token1 is JObject obj1 && token2 is JObject obj2)
    {
        var allKeys = new HashSet<string>(obj1.Properties().Select(p => p.Name)
                                     .Concat(obj2.Properties().Select(p => p.Name)));

        foreach (var key in allKeys.OrderBy(k => k))
        {
            DiffJTokens(obj1[key], obj2[key], $"{path}/{key}");
        }
    }
    else if (token1 is JArray arr1 && token2 is JArray arr2)
    {
        if (arr1.Count > 0 && arr1.First() is JObject o1 && o1["name"] != null && o1["in"] != null)
        {
            var dict1 = arr1.Children<JObject>().ToDictionary(x => $"{x["in"]}::{x["name"]}", x => x);
            var dict2 = arr2.Children<JObject>().ToDictionary(x => $"{x["in"]}::{x["name"]}", x => x);

            var allKeys = new HashSet<string>(dict1.Keys.Concat(dict2.Keys));

            foreach (var key in allKeys.OrderBy(k => k))
            {
                dict1.TryGetValue(key, out var v1);
                dict2.TryGetValue(key, out var v2);
                DiffJTokens(v1, v2, $"{path}[{key}]");
            }
        }
        else if (arr1.All(x => x.Type == JTokenType.String) && arr2.All(x => x.Type == JTokenType.String))
        {
            var set1 = new HashSet<string>(arr1.Select(x => x.ToString()));
            var set2 = new HashSet<string>(arr2.Select(x => x.ToString()));

            foreach (var val in set1.Union(set2).OrderBy(x => x))
            {
                if (set1.Contains(val) && set2.Contains(val))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"= {path}[]: \"{val}\"");
                }
                else if (set1.Contains(val))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"- {path}[]: \"{val}\"");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"+ {path}[]: \"{val}\"");
                }
                Console.ResetColor();
            }
        }
        else
        {
            var max = Math.Max(arr1.Count, arr2.Count);
            for (int i = 0; i < max; i++)
            {
                var v1 = i < arr1.Count ? arr1[i] : null;
                var v2 = i < arr2.Count ? arr2[i] : null;
                DiffJTokens(v1, v2, $"{path}[{i}]");
            }
        }
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"- {path}: {token1?.ToString(Formatting.None)}");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"+ {path}: {token2?.ToString(Formatting.None)}");
        Console.ResetColor();
    }
}

static JToken NormalizeJToken(JToken token)
{
    if (token is JObject obj)
    {
        return new JObject(
            obj.Properties()
               .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
               .Select(p => new JProperty(p.Name, NormalizeJToken(p.Value)))
        );
    }
    else if (token is JArray array)
    {
        if (!array.Any()) return array;

        if (array.First() is JObject firstObj)
        {
            if (firstObj["name"] != null && firstObj["in"] != null)
            {
                // parameters[]
                return new JArray(array.Children<JObject>()
                    .OrderBy(p => $"{p["in"]}::{p["name"]}", StringComparer.OrdinalIgnoreCase)
                    .Select(NormalizeJToken));
            }

            // fallback : tri JSON object par sérialisation pour consistance
            return new JArray(array.Children<JObject>()
                .OrderBy(obj => JsonConvert.SerializeObject(obj))
                .Select(NormalizeJToken));
        }

        // Cas des tableaux de strings simples (tags, consumes, produces, etc.)
        if (array.First() is JValue)
        {
            return new JArray(array.OrderBy(x => x.ToString(), StringComparer.OrdinalIgnoreCase));
        }

        // fallback
        return new JArray(array.Select(NormalizeJToken));
    }
    else
    {
        return token.DeepClone();
    }
}

static JToken SortJToken(JToken token)
{
    if (token is JObject obj)
    {
        var sorted = new JObject(
            obj.Properties()
               .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
               .Select(p => new JProperty(p.Name, SortJToken(p.Value)))
        );
        return sorted;
    }
    else if (token is JArray array)
    {
        // Cas spécial : parameters[] triés par ("name", "in")
        if (array.Count > 0 && array[0] is JObject first &&
            first["name"] != null && first["in"] != null)
        {
            return new JArray(
                array.Children<JObject>()
                     .OrderBy(p => $"{p["in"]}::{p["name"]}", StringComparer.OrdinalIgnoreCase)
                     .Select(SortJToken)
            );
        }

        return new JArray(array.Select(SortJToken));
    }
    else
    {
        return token.DeepClone();
    }
}

void Affiche(string strOld,string strNew)
{
    var lines1 = strOld.Split('\n');
    var lines2 = strNew.Split('\n');

    int max = Math.Max(lines1.Length, lines2.Length);
    for (int i = 0; i < max; i++)
    {
        var l1 = i < lines1.Length ? lines1[i].TrimEnd() : "";
        var l2 = i < lines2.Length ? lines2[i].TrimEnd() : "";

        if (l1 == l2)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"     {l1}");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(l1))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"   - {l1}");
            }
            if (!string.IsNullOrWhiteSpace(l2))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"   + {l2}");
            }
        }
    }
    Console.ResetColor();
}
#endregion

//------------------------------------------------------------------------------------------------------------------------------

var folder = "c:\\temp\\";

var jsonOrig = $"{folder}swagger3.json";
var jsonDest = $"{folder}swaggerZZZ.json";

var jsonOrigTemp = $"{folder}Manuel Init.json";
var jsonTrigTemp = $"{folder}Manuel triggers.json";
var jsonSecuTemp = $"{folder}Manuel security.json";
var jsonInfoTemp = $"{folder}Manuel infos.json";
var jsonResponseGetOrgaTemp = $"{folder}Manuel Response GetOrganizations.json";
var jsonParametersUploadTemp = $"{folder}Manuel Parameters Upload Chunk.json";

var jsonCompare = $"{folder}fnlcd_finalcad-20fc1_openapidefinition.json";

string jsonFile = File.ReadAllText(jsonOrig);
JObject root = JObject.Parse(jsonFile);

SupprimerPathsSansOperationId(root);
ModifierMethodes(root);
ModifierConsumes(root);
ModifierEndpointPath(root);
AjouterInitPath(root,jsonOrigTemp);
AjouterTriggers(root, jsonTrigTemp);
SupprimerPathProperties(root);
SupprimerClassesEnErreur(root);
SupprimerClassesProperties(root);
RemplacerBlocsSecu(root,jsonSecuTemp);
RemplacerBlocsInfos(root,jsonInfoTemp);
RemplacerResponceGetOrga(root, jsonResponseGetOrgaTemp);
RemplacerParamsChunk(root, jsonParametersUploadTemp);

Controle(root);

//Compare(root, jsonCompare);

File.WriteAllText(jsonDest, root.ToString(Newtonsoft.Json.Formatting.Indented));
