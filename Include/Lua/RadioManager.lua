-- ===================================================================================
-- 2.1 - RADIO MANAGER : plays radio messages (text and audio)
-- ===================================================================================
briefingRoom.radioManager = { } -- Main radio manager table
briefingRoom.radioManager.ANSWER_DELAY = { 4, 6 } -- Min/max time to get a answer to a radio message, in seconds
briefingRoom.radioManager.enableAudioMessages = $ENABLEAUDIORADIOMESSAGES$ -- Should audio radio messages be played?

function briefingRoom.radioManager.getAnswerDelay()
  return math.randomFloat(briefingRoom.radioManager.ANSWER_DELAY[1], briefingRoom.radioManager.ANSWER_DELAY[2])
end

-- Estimates the time (in seconds) required for the player to read a message
function briefingRoom.radioManager.getReadingTime(message)
  message = message or ""
  messsage = tostring(message)

  return math.max(5.0, #message / 8.7) -- 10.7 letters per second, minimum length 3.0 seconds
end

function briefingRoom.radioManager.play(message, oggFile, delay, functionToRun, functionParameters)
  delay = delay or 0
  local argsTable = { ["message"] = message, ["oggFile"] = oggFile, ["functionToRun"] = functionToRun, ["functionParameters"] = functionParameters }

  if delay > 0 then -- a delay was provided, schedule the radio message
    timer.scheduleFunction(briefingRoom.radioManager.doRadioMessage, argsTable, timer.getTime() + delay)
  else -- no delay, play the message at once
    briefingRoom.radioManager.doRadioMessage(argsTable, nil)
  end
end

function briefingRoom.radioManager.doRadioMessage(args, time)
  if args.message ~= nil then -- a message was provided, print it
    args.message = tostring(args.message)
    local duration = briefingRoom.radioManager.getReadingTime(args.message)
    trigger.action.outTextForCoalition(briefingRoom.playerCoalition, args.message, duration, false)
  end

  if args.oggFile ~= nil and briefingRoom.radioManager.enableAudioMessages then -- a sound was provided and radio sounds are enabled, play it
    trigger.action.outSoundForCoalition(briefingRoom.playerCoalition, "resources/ogg/"..args.oggFile..".ogg")
  else -- else play the default sound
    trigger.action.outSoundForCoalition(briefingRoom.playerCoalition, "resources/ogg/Radio0.ogg")
  end

  if args.functionToRun ~= nil then -- a function was provided, run it
    args.functionToRun(args.functionParameters)
  end

  return nil -- disable scheduling, if any
end