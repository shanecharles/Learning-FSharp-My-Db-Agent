// This is a record. Think of a Tuple with named properties.
type Person = { Id: int; FirstName: string; LastName: string;}

// Discriminated Union. Think of an Enum with an attached Tuple
type DbOperation = 
    | Add of Person
    | Filter of (Person -> bool) * AsyncReplyChannel<list<Person> >
    | Delete of int * AsyncReplyChannel<bool>
    | Update of Person * AsyncReplyChannel<bool>

type DbResult =
    | Results of list<Person>
    | Error of string

type DbMessage = 
    | DbMessage of DbOperation * AsyncReplyChannel<(bool*list<Person> option)>

let personServerAgent = MailboxProcessor.Start(fun inbox ->
    let rec loop oldState =
        async {
             let! (m) = inbox.Receive()
             let newState = 
                 match m with 
                 | Add(p) -> p::oldState
                 | Filter(func, c) ->
                        
                     c.Reply(List.filter func oldState)
                     oldState
                 | Delete(id, c) ->
                    (List.filter (fun p -> p.Id <> id) oldState)
                 | Update(p, c) ->
                     match oldState |> List.exists (fun e -> e.Id = p.Id) with
                     | true ->
                         c.Reply(false)
                         oldState
                     | _    ->
                         c.Reply(true)
                         p :: (oldState |> List.filter (fun p1 -> p1.Id <> p.Id))

             return! loop newState
         }
    loop [])


let populate = 
    personServerAgent.Post(Add({Id=1;FirstName="Sarah";LastName="Smith"})) |> ignore
    personServerAgent.Post(Add({Id=2;FirstName="Kevin";LastName="Daily"})) |> ignore
    personServerAgent.Post(Add({Id=3;FirstName="Mance";LastName="Watson"})) |> ignore
    personServerAgent.Post(Add({Id=4;FirstName="Brett";LastName="Islander"})) |> ignore
    personServerAgent.Post(Add({Id=5;FirstName="Mary";LastName="Watson"})) |> ignore

let updatePerson person =
    personServerAgent.PostAndReply(fun c -> Update(person,c))

let deleteById id =
    personServerAgent.PostAndReply(fun c -> Delete(id,c))

let getByLastName name = 
    personServerAgent.PostAndReply(fun c -> Filter((fun r -> r.LastName.Equals(name)),c))

let getById id = 
    personServerAgent.PostAndReply(fun c -> Filter((fun r -> r.Id = id),c))
