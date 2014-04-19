// This is a record. Think of a Tuple with named properties.
type Person = { Id: int; FirstName: string; LastName: string;}

// Discriminated Union. Think of an Enum with an attached Tuple
type DbOperation = 
    | Add of Person
    | Filter of (Person -> bool)
    | Delete of int

type DbResult =
    | Results of list<Person>
    | Error of string

type DbMessage = 
    | DbMessage of DbOperation * AsyncReplyChannel<obj>

let personServerAgent = MailboxProcessor.Start(fun inbox ->
    let rec loop oldState =
        async {
             let! (DbMessage(m,c)) = inbox.Receive()
             let newState, result, msg = 
                 match m with 
                 | Add(p) -> (p::oldState, true, None)
                 | Filter(func) -> (oldState, true, Some((List.filter func oldState)))
                 | Delete(id) ->
                    let rec removeId id items (acc:List<Person>) = 
                        match items with 
                        | [] -> List.rev acc
                        | hd::tl ->
                            match hd.Id = id with
                            | true -> removeId id tl acc
                            | _ -> removeId id tl (hd::acc)
                    (removeId id oldState []), true, None

             c.Reply (result, msg)
             return! loop newState
         }
    loop [])


let populate = 
    personServerAgent.PostAndReply(fun c -> DbMessage(Add({Id=1;FirstName="Sarah";LastName="Smith"}),c)) |> ignore
    personServerAgent.PostAndReply(fun c -> DbMessage(Add({Id=2;FirstName="Kevin";LastName="Daily"}),c)) |> ignore
    personServerAgent.PostAndReply(fun c -> DbMessage(Add({Id=3;FirstName="Mance";LastName="Watson"}),c)) |> ignore
    personServerAgent.PostAndReply(fun c -> DbMessage(Add({Id=4;FirstName="Brett";LastName="Islander"}),c)) |> ignore
    personServerAgent.PostAndReply(fun c -> DbMessage(Add({Id=5;FirstName="Mary";LastName="Watson"}),c)) |> ignore


let deleteById id =
    personServerAgent.PostAndReply(fun c -> DbMessage(Delete(id),c))

let getByLastName name = 
    personServerAgent.PostAndReply(fun c -> DbMessage(Filter(fun r -> r.LastName.Equals(name)),c))

let getById id = 
    personServerAgent.PostAndReply(fun c -> DbMessage(Filter(fun r -> r.Id = id),c))
